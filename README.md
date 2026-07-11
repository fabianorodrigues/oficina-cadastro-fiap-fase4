# oficina-cadastro-fiap-fase4

Microsservico responsavel pelo cadastro de clientes, veiculos, funcionarios e pelo catalogo de servicos da Oficina.

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

## Build e testes locais

```powershell
dotnet build src\Oficina.Cadastro.Api\Oficina.Cadastro.Api.csproj
dotnet test
```

## Docker

```powershell
docker build -f docker/Dockerfile -t oficina-cadastro-api .
```

Este servico e consumido pelo `oficina-ordens-servico-fiap-fase4` via HTTP interno e faz parte do ambiente Docker Compose local descrito naquele repositorio.
