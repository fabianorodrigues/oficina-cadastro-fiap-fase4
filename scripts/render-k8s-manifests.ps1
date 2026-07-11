param(
    [string]$ConfigPath = "config/official.json",
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [Parameter(Mandatory = $true)][string]$RuntimeImage,
    [Parameter(Mandatory = $true)][string]$MigrationImage,
    [Parameter(Mandatory = $true)][string]$AwsRegion,
    [Parameter(Mandatory = $true)][ValidateSet("PodIdentity", "IRSA")][string]$WorkloadIdentityMode,
    [string]$RuntimeIrsaRoleArn,
    [string]$MigrationIrsaRoleArn,
    [Parameter(Mandatory = $true)][string]$MigrationJobName
)

$ErrorActionPreference = "Stop"

function Assert-KubernetesName([string]$Name, [string]$Field) {
    if ($Name -notmatch "^[a-z0-9]([-a-z0-9]*[a-z0-9])?$" -or $Name.Length -gt 63) {
        throw "$Field nao e um nome Kubernetes valido: $Name"
    }
}

function Assert-Image([string]$Image, [string]$Field, [bool]$Migration) {
    if ($Image -match ":(latest|dev|hml|prod)$") {
        throw "$Field nao pode usar tag latest/dev/hml/prod."
    }
    if ($Migration -and $Image -notmatch ":[0-9a-f]{7,40}-migration$") {
        throw "$Field deve usar tag <github-sha>-migration."
    }
    if (-not $Migration -and $Image -notmatch ":[0-9a-f]{7,40}$") {
        throw "$Field deve usar tag <github-sha>."
    }
}

$config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
Assert-KubernetesName $config.kubernetes.namespace "namespace"
Assert-KubernetesName $config.kubernetes.deploymentName "deploymentName"
Assert-KubernetesName $config.kubernetes.serviceName "serviceName"
Assert-KubernetesName $MigrationJobName "MigrationJobName"
Assert-Image $RuntimeImage "RuntimeImage" $false
Assert-Image $MigrationImage "MigrationImage" $true

if ($WorkloadIdentityMode -eq "PodIdentity" -and ($RuntimeIrsaRoleArn -or $MigrationIrsaRoleArn)) {
    throw "Pod Identity nao pode ser renderizado com role ARN IRSA."
}

if ($WorkloadIdentityMode -eq "IRSA") {
    if (-not $RuntimeIrsaRoleArn -or -not $MigrationIrsaRoleArn) {
        throw "IRSA exige RuntimeIrsaRoleArn e MigrationIrsaRoleArn."
    }
    if ($RuntimeIrsaRoleArn -notmatch "^arn:aws:iam::\d{12}:role/.+" -or $MigrationIrsaRoleArn -notmatch "^arn:aws:iam::\d{12}:role/.+") {
        throw "Role ARN IRSA invalido."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$replacements = @{
    "@AWS_REGION@" = $AwsRegion
    "@RUNTIME_IMAGE@" = $RuntimeImage
    "@MIGRATION_IMAGE@" = $MigrationImage
    "@MIGRATION_JOB_NAME@" = $MigrationJobName
    "@RUNTIME_IRSA_ANNOTATION@" = "{}"
    "@MIGRATION_IRSA_ANNOTATION@" = "{}"
}

if ($WorkloadIdentityMode -eq "IRSA") {
    $replacements["@RUNTIME_IRSA_ANNOTATION@"] = "{`"eks.amazonaws.com/role-arn`": `"$RuntimeIrsaRoleArn`"}"
    $replacements["@MIGRATION_IRSA_ANNOTATION@"] = "{`"eks.amazonaws.com/role-arn`": `"$MigrationIrsaRoleArn`"}"
}

$templates = @(
    "service-account.template.yaml",
    "secret-provider-class-runtime.template.yaml",
    "secret-provider-class-migration.template.yaml",
    "configmap.yaml",
    "service.yaml",
    "deployment.template.yaml",
    "migration-job.template.yaml"
)

foreach ($template in $templates) {
    $source = Join-Path "deploy/k8s" $template
    $targetName = $template -replace "\.template", ""
    $target = Join-Path $OutputDirectory $targetName
    $content = Get-Content -LiteralPath $source -Raw
    foreach ($key in ($replacements.Keys | Sort-Object)) {
        $content = $content.Replace($key, $replacements[$key])
    }
    if ($content -match "@[A-Z0-9_]+@") {
        throw "Placeholder pendente em $template."
    }
    if ($content -match "ConnectionString=|Password=|SecretString|get-secret-value") {
        throw "Manifest renderizado contem dado ou operacao proibida."
    }
    Set-Content -LiteralPath $target -Value $content -NoNewline
}

Write-Host "Manifests renderizados em $OutputDirectory."
