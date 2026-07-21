param(
    [Parameter(Mandatory = $true)]
    [string]$RuntimeImage,

    [Parameter(Mandatory = $true)]
    [string]$MigrationImage,

    [Parameter(Mandatory = $true)]
    [string]$AwsRegion,

    [Parameter(Mandatory = $true)]
    [string]$TaskExecutionRoleArn,

    [Parameter(Mandatory = $true)]
    [string]$TaskRoleArn,

    [string]$ConfigPath = "config/official.json"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Get-SsmValue([string]$Name) {
    $value = aws ssm get-parameter --name $Name --region $AwsRegion --query 'Parameter.Value' --output text
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($value) -or $value -eq "None") {
        throw "Missing or empty SSM parameter: $Name"
    }
    return $value.Trim()
}

function Get-SecretArn([string]$Name) {
    $arn = aws secretsmanager describe-secret --secret-id $Name --region $AwsRegion --query 'ARN' --output text
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($arn) -or $arn -eq "None") {
        throw "Secret not found: $Name"
    }
    return $arn.Trim()
}

function New-EnvironmentEntry([string]$Name, [string]$Value) {
    @{ name = $Name; value = $Value }
}

function New-SecretEntry([string]$Name, [string]$ValueFrom) {
    @{ name = $Name; valueFrom = $ValueFrom }
}

function Register-TaskDefinition {
    param(
        [string]$Family,
        [string]$ContainerName,
        [string]$Image,
        [array]$Environment,
        [array]$Secrets,
        [bool]$ExposePort,
        [string]$LogGroupName
    )

    $container = @{
        name             = $ContainerName
        image            = $Image
        essential        = $true
        environment      = $Environment
        secrets          = $Secrets
        logConfiguration = @{
            logDriver = "awslogs"
            options   = @{
                "awslogs-group"         = $LogGroupName
                "awslogs-region"        = $AwsRegion
                "awslogs-stream-prefix" = $Family
            }
        }
    }

    if ($ExposePort) {
        $container.portMappings = @(@{
            containerPort = $containerPort
            hostPort      = $containerPort
            protocol      = "tcp"
        })
    }

    $definition = @{
        family                   = $Family
        networkMode              = "awsvpc"
        requiresCompatibilities  = @("FARGATE")
        cpu                      = "512"
        memory                   = "1024"
        executionRoleArn         = $TaskExecutionRoleArn
        taskRoleArn              = $TaskRoleArn
        containerDefinitions     = @($container)
    }

    $path = Join-Path ([System.IO.Path]::GetTempPath()) "$Family-taskdef.json"
    $definition | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $path -Encoding utf8
    $arn = aws ecs register-task-definition --cli-input-json "file://$path" --region $AwsRegion --query 'taskDefinition.taskDefinitionArn' --output text
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($arn) -or $arn -eq "None") {
        throw "Failed to register task definition: $Family"
    }
    return $arn.Trim()
}

$config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$appName = [string]$config.application.name
$serviceName = [string]$config.ecs.serviceName
$containerName = [string]$config.ecs.containerName
$migrationContainerName = [string]$config.ecs.migrationContainerName
$containerPort = [int]$config.application.containerPort

$clusterName = Get-SsmValue $config.aws.clusterNameParameter
$subnet1 = Get-SsmValue $config.ecs.privateSubnet1Parameter
$subnet2 = Get-SsmValue $config.ecs.privateSubnet2Parameter
$securityGroup = Get-SsmValue $config.ecs.taskSecurityGroupParameter
$targetGroupArn = Get-SsmValue $config.ecs.targetGroupArnParameter
$logGroupName = Get-SsmValue $config.ecs.logGroupNameParameter
$runtimeSecretArn = Get-SecretArn $config.secrets.runtimeDatabase
$migrationSecretArn = Get-SecretArn $config.secrets.migrationDatabase
$desiredCount = [int]$config.ecs.desiredCount

$baseEnvironment = @(
    New-EnvironmentEntry "ASPNETCORE_ENVIRONMENT" "Production",
    New-EnvironmentEntry "AWS_REGION" $AwsRegion,
    New-EnvironmentEntry "Authentication__Mode" ""
)

$connectionStringName = "ConnectionStrings__OficinaCadastroDb"
$extraEnvironment = @()

if ($appName -eq "oficina-estoque") {
    $commandsUrl = Get-SsmValue $config.queues.commandsUrlParameter
    $commandsDlqUrl = Get-SsmValue $config.queues.commandsDlqUrlParameter
    $eventsUrl = Get-SsmValue $config.queues.eventsUrlParameter
    $eventsDlqUrl = Get-SsmValue $config.queues.eventsDlqUrlParameter
    $connectionStringName = "ConnectionStrings__OficinaEstoqueDb"
    $extraEnvironment = @(
        New-EnvironmentEntry "Messaging__Sqs__Enabled" "true",
        New-EnvironmentEntry "Messaging__Sqs__Region" $AwsRegion,
        New-EnvironmentEntry "Messaging__Sqs__CommandsQueueName" "oficina-estoque-comandos.fifo",
        New-EnvironmentEntry "Messaging__Sqs__CommandsDlqQueueName" "oficina-estoque-comandos-dlq.fifo",
        New-EnvironmentEntry "Messaging__Sqs__EventsQueueName" "oficina-ordens-eventos.fifo",
        New-EnvironmentEntry "Messaging__Sqs__CommandsQueueUrl" $commandsUrl,
        New-EnvironmentEntry "Messaging__Sqs__CommandsDlqQueueUrl" $commandsDlqUrl,
        New-EnvironmentEntry "Messaging__Sqs__EventsQueueUrl" $eventsUrl,
        New-EnvironmentEntry "Messaging__Sqs__EventsDlqQueueUrl" $eventsDlqUrl,
        New-EnvironmentEntry "Messaging__Sqs__ConsumerConcurrency" "$($config.queues.consumerConcurrency)",
        New-EnvironmentEntry "Messaging__Sqs__MaxMessages" "$($config.queues.maxMessagesPerReceive)",
        New-EnvironmentEntry "Messaging__Sqs__WaitTimeSeconds" "$($config.queues.waitTimeSeconds)",
        New-EnvironmentEntry "Messaging__Sqs__VisibilityTimeoutSeconds" "$($config.queues.visibilityTimeoutSeconds)"
    )
}

if ($appName -eq "oficina-ordens-servico") {
    $commandsUrl = Get-SsmValue $config.queues.commandsUrlParameter
    $commandsDlqUrl = Get-SsmValue $config.queues.commandsDlqUrlParameter
    $eventsUrl = Get-SsmValue $config.queues.eventsUrlParameter
    $eventsDlqUrl = Get-SsmValue $config.queues.eventsDlqUrlParameter
    $albDns = Get-SsmValue $config.services.cadastroBaseUrlParameter
    $connectionStringName = "ConnectionStrings__DefaultConnection"
    $extraEnvironment = @(
        New-EnvironmentEntry "DistributedFlow__Enabled" "true",
        New-EnvironmentEntry "Integrations__Cadastro__BaseUrl" "http://$albDns",
        New-EnvironmentEntry "Integrations__Estoque__BaseUrl" "http://$albDns",
        New-EnvironmentEntry "Messaging__Sqs__Enabled" "true",
        New-EnvironmentEntry "Messaging__Sqs__Region" $AwsRegion,
        New-EnvironmentEntry "Messaging__Sqs__CommandsQueueName" "oficina-estoque-comandos.fifo",
        New-EnvironmentEntry "Messaging__Sqs__EventsQueueName" "oficina-ordens-eventos.fifo",
        New-EnvironmentEntry "Messaging__Sqs__EventsDlqQueueName" "oficina-ordens-eventos-dlq.fifo",
        New-EnvironmentEntry "Messaging__Sqs__CommandsQueueUrl" $commandsUrl,
        New-EnvironmentEntry "Messaging__Sqs__CommandsDlqQueueUrl" $commandsDlqUrl,
        New-EnvironmentEntry "Messaging__Sqs__EventsQueueUrl" $eventsUrl,
        New-EnvironmentEntry "Messaging__Sqs__EventsDlqQueueUrl" $eventsDlqUrl,
        New-EnvironmentEntry "Messaging__Sqs__ConsumerConcurrency" "$($config.queues.consumerConcurrency)",
        New-EnvironmentEntry "Messaging__Sqs__MaxMessagesPerReceive" "$($config.queues.maxMessagesPerReceive)",
        New-EnvironmentEntry "Messaging__Sqs__WaitTimeSeconds" "$($config.queues.waitTimeSeconds)",
        New-EnvironmentEntry "Messaging__Sqs__VisibilityTimeoutSeconds" "$($config.queues.visibilityTimeoutSeconds)",
        New-EnvironmentEntry "Payments__UseMock" "$($config.payments.useMock)".ToLowerInvariant(),
        New-EnvironmentEntry "Payments__Mode" "Mock",
        New-EnvironmentEntry "Payments__MockBehavior" "$($config.payments.mockBehavior)",
        New-EnvironmentEntry "Payments__ExternalApiEnabled" "false",
        New-EnvironmentEntry "Payments__ExternalWebhookEnabled" "false",
        New-EnvironmentEntry "Payments__ContractStatus" "$($config.payments.contractStatus)"
    )
}

$runtimeEnvironment = @($baseEnvironment + $extraEnvironment)
$migrationEnvironment = @($baseEnvironment)
$runtimeSecrets = @(New-SecretEntry $connectionStringName "$($runtimeSecretArn):ConnectionString::")
$migrationSecrets = @(New-SecretEntry $connectionStringName "$($migrationSecretArn):ConnectionString::")

$migrationTaskDefinition = Register-TaskDefinition `
    -Family "$serviceName-migration" `
    -ContainerName $migrationContainerName `
    -Image $MigrationImage `
    -Environment $migrationEnvironment `
    -Secrets $migrationSecrets `
    -ExposePort $false `
    -LogGroupName $logGroupName

$networkConfiguration = "awsvpcConfiguration={subnets=[$subnet1,$subnet2],securityGroups=[$securityGroup],assignPublicIp=DISABLED}"
$migrationTask = aws ecs run-task --cluster $clusterName --launch-type FARGATE --started-by "$serviceName-migration" --task-definition $migrationTaskDefinition --network-configuration $networkConfiguration --region $AwsRegion --query 'tasks[0].taskArn' --output text
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($migrationTask) -or $migrationTask -eq "None") {
    throw "Failed to start migration task for $serviceName."
}
aws ecs wait tasks-stopped --cluster $clusterName --tasks $migrationTask --region $AwsRegion
if ($LASTEXITCODE -ne 0) { throw "Migration task wait failed for $serviceName." }
$migrationExit = aws ecs describe-tasks --cluster $clusterName --tasks $migrationTask --region $AwsRegion --query "tasks[0].containers[?name=='$migrationContainerName'].exitCode | [0]" --output text
if ($migrationExit -ne "0") { throw "Migration task failed for $serviceName with exit code $migrationExit." }

$runtimeTaskDefinition = Register-TaskDefinition `
    -Family $serviceName `
    -ContainerName $containerName `
    -Image $RuntimeImage `
    -Environment $runtimeEnvironment `
    -Secrets $runtimeSecrets `
    -ExposePort $true `
    -LogGroupName $logGroupName

$existingStatus = aws ecs describe-services --cluster $clusterName --services $serviceName --region $AwsRegion --query 'services[0].status' --output text 2>$null
if ($LASTEXITCODE -eq 0 -and $existingStatus -eq "ACTIVE") {
    aws ecs update-service --cluster $clusterName --service $serviceName --task-definition $runtimeTaskDefinition --desired-count $desiredCount --region $AwsRegion | Out-Null
}
else {
    aws ecs create-service `
        --cluster $clusterName `
        --service-name $serviceName `
        --task-definition $runtimeTaskDefinition `
        --desired-count $desiredCount `
        --launch-type FARGATE `
        --network-configuration $networkConfiguration `
        --load-balancers "targetGroupArn=$targetGroupArn,containerName=$containerName,containerPort=$containerPort" `
        --health-check-grace-period-seconds 60 `
        --region $AwsRegion | Out-Null
}

aws ecs wait services-stable --cluster $clusterName --services $serviceName --region $AwsRegion
if ($LASTEXITCODE -ne 0) { throw "ECS service did not become stable: $serviceName" }

$healthyTargets = aws elbv2 describe-target-health --target-group-arn $targetGroupArn --region $AwsRegion --query 'length(TargetHealthDescriptions[?TargetHealth.State==`healthy`])' --output text
if ($LASTEXITCODE -ne 0 -or [int]$healthyTargets -lt 1) {
    throw "No healthy ALB target found for $serviceName."
}

Write-Host "ECS deployment completed for $serviceName."
Write-Host "Migration task: $migrationTask"
Write-Host "Runtime task definition: $runtimeTaskDefinition"
