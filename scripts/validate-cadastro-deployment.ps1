param(
    [Parameter(Mandatory = $true)][string]$ExpectedImageTag,
    [string]$Namespace = "oficina",
    [string]$DeploymentName = "oficina-cadastro",
    [string]$ServiceName = "oficina-cadastro",
    [string]$MigrationJobName
)

$ErrorActionPreference = "Stop"

if ($ExpectedImageTag -notmatch "^[0-9a-f]{7,40}$") {
    throw "ExpectedImageTag deve ser um SHA Git curto ou completo."
}

aws sts get-caller-identity | Out-Null

$deployment = kubectl get deployment $DeploymentName -n $Namespace -o json | ConvertFrom-Json
$service = kubectl get service $ServiceName -n $Namespace -o json | ConvertFrom-Json
$hpa = kubectl get hpa -n $Namespace -o name --ignore-not-found

if ($deployment.spec.replicas -ne 1) { throw "Deployment deve desejar 1 replica." }
if ($deployment.status.readyReplicas -ne 1) { throw "Deployment deve possuir 1 replica Ready." }
if ($deployment.spec.template.spec.serviceAccountName -ne "cadastro-runtime") { throw "ServiceAccount runtime incorreta." }
if ($deployment.spec.template.spec.containers[0].image -notmatch ":$ExpectedImageTag$") { throw "Imagem runtime nao corresponde ao SHA esperado." }
if ($service.spec.type -ne "ClusterIP") { throw "Service deve ser ClusterIP." }
if ($hpa) { throw "Nao deve existir HPA para oficina-cadastro." }

kubectl get secretproviderclass oficina-cadastro-runtime-db -n $Namespace | Out-Null
kubectl rollout status deployment/$DeploymentName -n $Namespace --timeout=120s | Out-Null

if ($MigrationJobName) {
    $job = kubectl get job $MigrationJobName -n $Namespace -o json | ConvertFrom-Json
    if ($job.status.succeeded -ne 1) { throw "Migration Job nao concluido." }
}

Write-Host "Deployment Cadastro validado em modo read-only."
