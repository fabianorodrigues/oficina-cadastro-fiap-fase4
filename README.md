# oficina-cadastro-fiap-fase4

## Responsabilidade

Microsservico responsavel pelo cadastro de clientes, veiculos, funcionarios e pelo catalogo de servicos da Oficina. Independente dos demais microsservicos: CI, deploy e banco lógico (`OficinaCadastroDb`) próprios. Ponto de entrada da solução: [oficina-infra-fiap-fase4](../oficina-infra-fiap-fase4/README.md).

## Arquitetura

Clean Architecture com 4 camadas:

- `Oficina.Cadastro.Domain` — entidades, agregados e value objects (`Clientes`, `Veiculos`, `CatalogoEstoque`, `Seguranca`); sem dependencias externas.
- `Oficina.Cadastro.Application` — casos de uso, DTOs e validators, organizados por contexto (`Clientes`, `Veiculos`, `CatalogoEstoque`, `Seguranca`, `Internal`).
- `Oficina.Cadastro.Infrastructure` — persistencia EF Core (`CadastroDbContext`), repositories e migrations.
- `Oficina.Cadastro.Api` — controllers, autenticacao/autorizacao, middlewares e composition root (`Program.cs`).

## Endpoints principais

- `api/clientes` — CRUD de clientes.
- `api/veiculos` — CRUD de veiculos.
- `api/servicos` — catalogo de servicos.
- `api/admin/funcionarios` — gestao de funcionarios (perfil Admin).
- `api/internal/*` — consultas usadas pelo servico de Ordens de Servico (clientes, veiculos e servicos em lote).

Autenticacao em ambiente local via header scheme (`Authentication:Mode=Development`), bloqueada fora de `Development`.

## Configuracao local

Em `Development`, a API preserva o fluxo local atual:

- `Authentication:Mode=Development` habilita o scheme de desenvolvimento.
- `ConnectionStrings:OficinaCadastroDb` usa SQL Server local por padrao.
- `Database:ApplyMigrations=true` pode aplicar migrations somente em `Development`.
- `/health` valida liveness do processo.
- `/ready` valida readiness incluindo conexao com o banco.

Em `Production`, a API:

- exige `ASPNETCORE_ENVIRONMENT=Production`;
- le secrets montados em `/mnt/secrets-store` por `AddKeyPerFile`;
- espera a chave `ConnectionStrings__OficinaCadastroDb`;
- rejeita `Authentication__Mode=Development`;
- nao executa migrations automaticas no runtime;
- falha sem connection string obrigatoria.

## Publicacao oficial

A configuracao oficial versionada esta em `config/official.json` e nao contem dados sensiveis.

Arquitetura de publicacao:

```text
ECR
-> Migration Job
-> Deployment
-> ClusterIP
```

Secrets consumidos via AWS Secrets Manager e CSI:

- `/oficina/cadastro/runtime-db`: usado pelo runtime com usuario `cadastro_app`.
- `/oficina/cadastro/migration-db`: usado pelo Migration Job com usuario `cadastro_migrator`.

As senhas nao sao configuradas neste repositorio. Os valores vem do fluxo centralizado de Infra DB, e nenhum secret SQL deve ser duplicado em GitHub Secrets.

Execucao futura do deploy:

```text
GitHub
-> Actions
-> Cadastro Deploy
-> Run workflow
-> Branch main
-> confirmation DEPLOY
```

Dependencias externas para execucao real:

- Backend Terraform
- Infra DB
- Platform
- Database Infrastructure Deploy
- Database Bootstrap
- ECR
- EKS
- CSI
- ASCP
- Pod Identity ou IRSA

Sem acesso a AWS, build e validacoes locais podem ser concluidos. Push, migration, rollout e smoke test no cluster dependem de credenciais AWS configuradas e das dependencias acima provisionadas.

## CI/CD

- CI principal: `Cadastro CI`, executada em todo Pull Request para `main`.
- Required check esperado na branch protection: `Cadastro CI`.
- Workflow manual: `Cadastro Deploy`, executado somente por `workflow_dispatch` na `main`, com confirmation `DEPLOY`.
- Repository Secrets usados pelo deploy: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`.
- Repository Variables usadas pelo deploy: `AWS_REGION`.
- A CI nao recebe credenciais AWS, nao publica imagens e nao altera AWS ou Kubernetes.
- O deploy valida branch, confirmacao, configuracao oficial, STS, SSM, Secrets Manager por metadata, ECR, cluster, migration, rollout, health e readiness.
- Tags de imagem: `${GITHUB_SHA}` para runtime e `${GITHUB_SHA}-migration` para migration.
- Nao existe pipeline dedicada de rollback ou destroy. Para corrigir uma entrega, reverta ou ajuste o codigo em nova branch, abra Pull Request, aguarde `Cadastro CI`, faca merge na `main` e execute novamente `Cadastro Deploy`.
- Branch protection recomendada: Pull Request obrigatorio, required check `Cadastro CI`, bloqueio de force push e bloqueio de delecao da branch. Segundo revisor nao e obrigatorio para execucao individual ou dupla.

## Build e testes locais

```powershell
dotnet restore Oficina.Cadastro.sln
dotnet build Oficina.Cadastro.sln -c Release --no-restore
dotnet test Oficina.Cadastro.sln -c Release --no-build
pwsh scripts\validate-official-config.ps1
```

## Docker

```powershell
docker build -f Dockerfile -t oficina-cadastro:local .
docker build -f Dockerfile.migration -t oficina-cadastro:local-migration .
```

Este servico e consumido pelo `oficina-ordens-servico-fiap-fase4` via HTTP interno e faz parte do ambiente Docker Compose local descrito naquele repositorio.

O Dockerfile oficial da raiz gera uma imagem runtime sem SDK e sem `dotnet-ef`. O `Dockerfile.migration` gera um EF Migration Bundle em `/app/efbundle`; ele nao executa migrations durante o build e recebe a connection string somente em runtime.

## Próximo componente

Cadastro é implantado de forma independente após as dependências compartilhadas (Infra DB, Platform, Auth). Após o deploy, o [oficina-ordens-servico-fiap-fase4](../oficina-ordens-servico-fiap-fase4/README.md) o consome via HTTP interno.
