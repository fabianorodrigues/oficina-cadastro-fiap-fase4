# Kubernetes - oficina-cadastro

Este diretorio contem manifests e templates para publicacao independente do microservico Cadastro no namespace `oficina`.

## Arquitetura

```text
ECR
-> Migration Job
-> Deployment
-> Service ClusterIP
```

## Recursos

- `service-account.template.yaml`: ServiceAccounts separados para runtime (`cadastro-runtime`) e migration (`cadastro-migrator`).
- `secret-provider-class-runtime.template.yaml`: monta somente `ConnectionString` de `/oficina/cadastro/runtime-db` como `ConnectionStrings__OficinaCadastroDb`.
- `secret-provider-class-migration.template.yaml`: monta somente `ConnectionString` de `/oficina/cadastro/migration-db` como `ConnectionStrings__OficinaCadastroDb`.
- `configmap.yaml`: contem apenas configuracoes nao sensiveis.
- `service.yaml`: cria `oficina-cadastro` como `ClusterIP`.
- `deployment.template.yaml`: publica a API com 1 replica, probes `/health` e `/ready`, volume CSI e security context restritivo.
- `migration-job.template.yaml`: executa `/app/efbundle` antes do Deployment usando a imagem `<github-sha>-migration`.

## Secrets

Secrets oficiais:

- `/oficina/cadastro/runtime-db`
- `/oficina/cadastro/migration-db`

As senhas nao sao configuradas neste repositorio. Os valores vem do fluxo centralizado de Infra DB. Runtime e migration usam usuarios SQL diferentes, e nenhum secret SQL deve ser duplicado no GitHub.

## Renderizacao

Exemplo com Pod Identity:

```powershell
pwsh scripts/render-k8s-manifests.ps1 `
  -OutputDirectory ./.rendered/podidentity `
  -RuntimeImage 000000000000.dkr.ecr.us-east-1.amazonaws.com/oficina-cadastro:0123456789abcdef `
  -MigrationImage 000000000000.dkr.ecr.us-east-1.amazonaws.com/oficina-cadastro:0123456789abcdef-migration `
  -AwsRegion us-east-1 `
  -WorkloadIdentityMode PodIdentity `
  -MigrationJobName oficina-cadastro-migration-123456789-1
```

Exemplo com IRSA sintetico:

```powershell
pwsh scripts/render-k8s-manifests.ps1 `
  -OutputDirectory ./.rendered/irsa `
  -RuntimeImage 000000000000.dkr.ecr.us-east-1.amazonaws.com/oficina-cadastro:0123456789abcdef `
  -MigrationImage 000000000000.dkr.ecr.us-east-1.amazonaws.com/oficina-cadastro:0123456789abcdef-migration `
  -AwsRegion us-east-1 `
  -WorkloadIdentityMode IRSA `
  -RuntimeIrsaRoleArn arn:aws:iam::000000000000:role/oficina-cadastro-runtime `
  -MigrationIrsaRoleArn arn:aws:iam::000000000000:role/oficina-cadastro-migrator `
  -MigrationJobName oficina-cadastro-migration-123456789-1
```

Pod Identity e IRSA nao devem ser habilitados simultaneamente. Este repositorio nao cria IAM.

## Execucao futura

Deploy:

```text
GitHub -> Actions -> Cadastro Deploy -> Run workflow -> Branch main -> confirmation DEPLOY
```

Correcao de entrega:

```text
Nova branch -> Pull Request -> merge na main -> executar novamente Cadastro Deploy
```

Nao existe pipeline dedicada para desfazer publicacao. As imagens continuam versionadas pelo SHA do Git.

## Validação sem acesso à AWS

Build e validacoes estaticas podem ser concluidos localmente. STS, SSM real, ECR real, Secrets Manager metadata real, Pod Identity/IRSA real, CSI/ASCP real, push, Migration Job, rollout e smoke test dependem de credenciais AWS configuradas.
