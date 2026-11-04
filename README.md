## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (banco local e testes de integração)

## Execução

```bash
docker compose up -d
dotnet run --project src/Transferencias.Api
```

A API sobe em `http://localhost:5000`. Na inicialização, o banco é criado com a conta **A** (100000 centavos) e a conta **B** (50000 centavos).

## Testes

```bash
dotnet test
```

Os testes de integração sobem um PostgreSQL descartável com Testcontainers, então o Docker precisa estar rodando. Não é preciso executar o `docker compose` antes.

## Referências e ferramentas

- [.NET 10 / ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) e [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [PostgreSQL 18](https://www.postgresql.org/docs/18/)
- [xUnit](https://xunit.net/), [WebApplicationFactory](https://learn.microsoft.com/aspnet/core/test/integration-tests) e [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- Docker
