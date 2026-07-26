param(
    [string]$ConfigPath = "config/official.json",
    [string]$ManifestDirectory = "k8s"
)

$ErrorActionPreference = "Stop"
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$Message) {
    $script:errors.Add($Message)
}

function Get-ConfigMapValue {
    param(
        [Parameter(Mandatory = $true)][string[]]$Lines,
        [Parameter(Mandatory = $true)][string]$Key
    )

    foreach ($line in $Lines) {
        if ($line -match "^\s+$([regex]::Escape($Key))\s*:\s*(.*)$") {
            return $Matches[1].Trim().Trim('"')
        }
    }

    return $null
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
if ($null -ne $config.PSObject.Properties['ecs']) { Add-Error "bloco ecs removido: use kubernetes." }
if ($config.kubernetes.deploymentName -ne "oficina-cadastro") { Add-Error "kubernetes.deploymentName invalido." }
if ($config.kubernetes.serviceName -ne "oficina-cadastro") { Add-Error "kubernetes.serviceName invalido." }
if ($config.kubernetes.containerName -ne "oficina-cadastro") { Add-Error "kubernetes.containerName invalido." }
if ($config.kubernetes.migrationJobPrefix -ne "oficina-cadastro-migration") { Add-Error "kubernetes.migrationJobPrefix invalido." }
if ($config.kubernetes.replicas -ne 1) { Add-Error "replicas deve ser 1." }
if ($config.kubernetes.nodePort -lt 30000 -or $config.kubernetes.nodePort -gt 32767) { Add-Error "nodePort fora da faixa 30000-32767." }
foreach ($manifestKey in @('configMap', 'deployment', 'service', 'migrationJob', 'secretApp', 'secretMigration')) {
    $manifestPath = $config.kubernetes.manifests.$manifestKey
    if ([string]::IsNullOrWhiteSpace($manifestPath) -or -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        Add-Error "manifesto ausente: $manifestKey ($manifestPath)"
    }
}
# Um Secret unico servindo Deployment e Job daria ao runtime a credencial de
# migration; os dois templates precisam ser arquivos distintos.
if ($config.kubernetes.manifests.secretApp -eq $config.kubernetes.manifests.secretMigration) {
    Add-Error "secretApp e secretMigration devem ser manifests distintos."
}
if ($config.deploy.s3Prefix -ne "k8s-deploy/cadastro/") { Add-Error "deploy.s3Prefix invalido." }
if ($config.deploy.presignedUrlTtlSeconds -gt 300 -or $config.deploy.presignedUrlTtlSeconds -le 0) { Add-Error "deploy.presignedUrlTtlSeconds deve ficar entre 1 e 300." }
if ($config.coverage.minimumLinePercentage -lt 80) { Add-Error "cobertura minima deve ser ao menos 80." }
if ($config.secrets.runtimeDatabase -ne "/oficina/cadastro/runtime-db") { Add-Error "secret runtime invalido." }
if ($config.secrets.migrationDatabase -ne "/oficina/cadastro/migration-db") { Add-Error "secret migration invalido." }
if ($config.secrets.runtimeDatabase -eq $config.secrets.migrationDatabase) { Add-Error "runtime e migration devem usar secrets diferentes." }
if (-not $config.secrets.runtimeDatabase.StartsWith("/oficina/")) { Add-Error "secret runtime deve iniciar com /oficina/." }
if (-not $config.secrets.migrationDatabase.StartsWith("/oficina/")) { Add-Error "secret migration deve iniciar com /oficina/." }
if ($config.health.path -ne "/health") { Add-Error "health.path invalido." }
if ($config.health.readinessPath -ne "/ready") { Add-Error "health.readinessPath invalido." }

$paths = @(
    $config.aws.namespaceParameter,
    $config.aws.instanceIdParameter,
    $config.aws.ecrRepositoryParameter,
    $config.kubernetes.targetGroupArnParameter,
    $config.kubernetes.nodePortParameter,
    $config.secrets.runtimeDatabase,
    $config.secrets.migrationDatabase,
    $config.deploy.parameterPathPrefix
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
    "(^|[\/_\-])(dev|hml|staging|prod)([\/_\-]|$)"
)

foreach ($pattern in $forbiddenPatterns) {
    if ($raw -match $pattern) {
        Add-Error "config contem padrao proibido: $pattern"
    }
}

# ---------------------------------------------------------------------------
# Contrato de observabilidade dos ConfigMaps.
#
# Tres invariantes que so apareceriam em runtime se nao fossem verificadas aqui:
#
# 1. OpenTelemetry__OtlpEndpoint e o gate que registra o exporter e
#    OTEL_EXPORTER_OTLP_ENDPOINT e o valor que o SDK realmente usa. Divergindo, a
#    aplicacao registra o exporter apontando para outro destino.
# 2. service.version nao pode ter duas origens. Fica somente em
#    OTEL_SERVICE_VERSION; repetido em OTEL_RESOURCE_ATTRIBUTES, as duas fontes
#    divergem em silencio.
# 3. Nenhuma credencial da New Relic pode entrar no Pod. OTEL_EXPORTER_OTLP_HEADERS
#    entra na lista porque e por ele que a license key chegaria ao exporter.
# ---------------------------------------------------------------------------

$forbiddenTelemetryKeys = @(
    'NEW_RELIC_LICENSE_KEY',
    'NEW_RELIC_USER_API_KEY',
    'NEW_RELIC_API_KEY',
    'OTEL_EXPORTER_OTLP_HEADERS'
)

$telemetryFound = $false

if (Test-Path -LiteralPath $ManifestDirectory) {
    foreach ($manifest in Get-ChildItem -LiteralPath $ManifestDirectory -Filter '*.yaml' -File) {
        $lines = Get-Content -LiteralPath $manifest.FullName
        $name = $manifest.Name

        foreach ($key in $forbiddenTelemetryKeys) {
            if ($lines | Select-String -Pattern "^\s+$([regex]::Escape($key))\s*:" -Quiet) {
                Add-Error "$name declara $key. Somente o Collector conhece credencial da New Relic."
            }
        }

        foreach ($pattern in @('NRAK-[A-Za-z0-9]{10,}', 'NRAA-[A-Za-z0-9]{10,}')) {
            if ($lines | Select-String -Pattern $pattern -Quiet) {
                Add-Error "$name contem valor com formato de chave da New Relic ($pattern)."
            }
        }

        $gate = Get-ConfigMapValue -Lines $lines -Key 'OpenTelemetry__OtlpEndpoint'
        $sdk = Get-ConfigMapValue -Lines $lines -Key 'OTEL_EXPORTER_OTLP_ENDPOINT'
        if ($null -eq $gate -and $null -eq $sdk) { continue }

        $telemetryFound = $true

        if ($null -eq $gate) {
            Add-Error "$name define OTEL_EXPORTER_OTLP_ENDPOINT sem OpenTelemetry__OtlpEndpoint: sem o gate nenhum exporter e registrado."
        }
        elseif ($null -eq $sdk) {
            Add-Error "$name define OpenTelemetry__OtlpEndpoint sem OTEL_EXPORTER_OTLP_ENDPOINT: o SDK cairia no destino default."
        }
        elseif ($gate -ne $sdk) {
            Add-Error "$name tem endpoints de telemetria divergentes: gate '$gate' e SDK '$sdk'."
        }

        if ([string]::IsNullOrWhiteSpace((Get-ConfigMapValue -Lines $lines -Key 'OTEL_SERVICE_NAME'))) {
            Add-Error "$name nao define OTEL_SERVICE_NAME."
        }

        if ([string]::IsNullOrWhiteSpace((Get-ConfigMapValue -Lines $lines -Key 'OTEL_SERVICE_VERSION'))) {
            Add-Error "$name nao define OTEL_SERVICE_VERSION."
        }

        $attributes = Get-ConfigMapValue -Lines $lines -Key 'OTEL_RESOURCE_ATTRIBUTES'
        if ($null -ne $attributes) {
            if ($attributes -match 'service\.version\s*=') {
                Add-Error "$name repete service.version em OTEL_RESOURCE_ATTRIBUTES. A unica origem e OTEL_SERVICE_VERSION."
            }

            foreach ($required in @('deployment.environment', 'service.namespace', 'k8s.cluster.name')) {
                if ($attributes -notmatch "$([regex]::Escape($required))\s*=") {
                    Add-Error "$name nao declara $required em OTEL_RESOURCE_ATTRIBUTES."
                }
            }
        }
    }

    if (-not $telemetryFound) {
        Add-Error "Nenhum manifesto em $ManifestDirectory declara a configuracao de telemetria."
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Configuracao oficial e contrato de observabilidade validos."
