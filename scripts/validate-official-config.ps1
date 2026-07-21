param(
    [string]$ConfigPath = "config/official.json"
)

$ErrorActionPreference = "Stop"
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$Message) {
    $script:errors.Add($Message)
}

if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Arquivo de configuracao oficial nao encontrado: $ConfigPath"
}

$raw = Get-Content -LiteralPath $ConfigPath -Raw
$config = $raw | ConvertFrom-Json

if ($config.version -ne 1) { Add-Error "version deve ser 1." }
if ($config.application.name -ne "oficina-cadastro") { Add-Error "application.name invalido." }
if ($config.application.environment -ne "Production") { Add-Error "application.environment deve ser Production." }
if ($config.application.containerPort -lt 1 -or $config.application.containerPort -gt 65535) { Add-Error "containerPort invalido." }
if ($config.ecs.serviceName -ne "oficina-cadastro") { Add-Error "ecs.serviceName invalido." }
if ($config.ecs.containerName -ne "oficina-cadastro") { Add-Error "ecs.containerName invalido." }
if ($config.ecs.migrationContainerName -ne "oficina-cadastro-migration") { Add-Error "ecs.migrationContainerName invalido." }
if ($config.ecs.desiredCount -ne 1) { Add-Error "desiredCount deve ser 1." }
if ($config.ecs.launchType -ne "FARGATE") { Add-Error "launchType deve ser FARGATE." }
if ($config.secrets.runtimeDatabase -ne "/oficina/cadastro/runtime-db") { Add-Error "secret runtime invalido." }
if ($config.secrets.migrationDatabase -ne "/oficina/cadastro/migration-db") { Add-Error "secret migration invalido." }
if ($config.secrets.runtimeDatabase -eq $config.secrets.migrationDatabase) { Add-Error "runtime e migration devem usar secrets diferentes." }
if (-not $config.secrets.runtimeDatabase.StartsWith("/oficina/")) { Add-Error "secret runtime deve iniciar com /oficina/." }
if (-not $config.secrets.migrationDatabase.StartsWith("/oficina/")) { Add-Error "secret migration deve iniciar com /oficina/." }
if ($config.health.path -ne "/health") { Add-Error "health.path invalido." }
if ($config.health.readinessPath -ne "/ready") { Add-Error "health.readinessPath invalido." }

$paths = @(
    $config.aws.clusterNameParameter,
    $config.aws.ecrRepositoryParameter,
    $config.ecs.targetGroupArnParameter,
    $config.ecs.logGroupNameParameter,
    $config.ecs.taskSecurityGroupParameter,
    $config.ecs.privateSubnet1Parameter,
    $config.ecs.privateSubnet2Parameter,
    $config.secrets.runtimeDatabase,
    $config.secrets.migrationDatabase
)
foreach ($path in $paths) {
    if ([string]::IsNullOrWhiteSpace($path) -or -not $path.StartsWith("/oficina/")) {
        Add-Error "path fora do prefixo /oficina/: $path"
    }
}

$forbiddenPatterns = @(
    "Password\s*=",
    "ConnectionString\s*=",
    "Server=tcp:",
    "\d{12}\.dkr\.ecr\.",
    "arn:aws:secretsmanager:",
    "AKIA[0-9A-Z]{16}",
    "(^|[\/_\-])(dev|hml|staging|prod)([\/_\-]|$)",
    "Fase3|fase-3"
)

foreach ($pattern in $forbiddenPatterns) {
    if ($raw -match $pattern) {
        Add-Error "config contem padrao proibido: $pattern"
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Configuracao oficial valida."
