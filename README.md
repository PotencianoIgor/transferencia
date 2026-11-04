# Transferências entre contas

API em C# com ASP.NET Core para transferências entre duas contas fictícias, com persistência em PostgreSQL e acesso assíncrono via Entity Framework Core. Todos os dados são fictícios.

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (banco local e testes de integração)

## Execução

```bash
docker compose up -d
dotnet run --project src/Transferencias.Api
```

A API sobe em `http://localhost:5000`. Na inicialização, o banco é criado com a conta **A** (100000 centavos) e a conta **B** (50000 centavos).

### Exemplos

```bash
curl -i -X POST http://localhost:5000/transferencias \
  -H "Content-Type: application/json" \
  -d '{"origem":"A","destino":"B","valorCentavos":30000}'

curl -i http://localhost:5000/contas/A

curl -i http://localhost:5000/contas/A/movimentacoes
```

| Operação | Sucesso | Erros |
|---|---|---|
| `POST /transferencias` | 201 com a transferência (`id`, `origem`, `destino`, `valorCentavos`) | 400 entrada inválida, 404 conta inexistente, 409 saldo insuficiente |
| `GET /contas/{id}` | 200 com `id` e `saldoCentavos` | 404 |
| `GET /contas/{id}/movimentacoes` | 200 com as transferências em que a conta é origem ou destino | 404 |

## Testes

```bash
dotnet test
```

Os testes de integração sobem um PostgreSQL descartável com Testcontainers, então o Docker precisa estar rodando. Não é preciso executar o `docker compose` antes.

## Estrutura

```
src/
  Transferencias.Domain          Conta, Transferencia, TransferenciaService, IContaRepository
  Transferencias.Infrastructure  AppDbContext e ContaRepository (EF Core + Npgsql)
  Transferencias.Api             Endpoints e mapeamento das exceções para status HTTP
tests/
  Transferencias.Tests
    TransferenciaServiceTests    Regras de negócio com repositório fake
    ApiTests                     HTTP + PostgreSQL real: status, saldos, movimentações e falha sem efeito parcial
```

## Decisões

- **Valores em centavos (`long`)**, sem ponto flutuante.
- **Regras no domínio:** `Conta.Debitar` recusa saldo insuficiente e `TransferenciaService` valida a entrada e orquestra a operação.
- **Atomicidade:** débito, crédito e registro acontecem numa única transação. Se qualquer passo falhar antes do commit, a transação é desfeita. O teste `Falha_antes_de_confirmar_nao_deixa_transferencia_parcial` substitui o repositório por um que falha na confirmação e verifica que saldos e movimentações não mudaram.
- **Substituição de dependências:** o domínio depende de `IContaRepository` e `ITransacao`, sem conhecer o EF Core. Os testes de regra usam um repositório fake.
- **Banco criado com `EnsureCreated`**, sem migrations, porque o escopo não exige evolução de esquema.

## Referências e ferramentas

- [.NET 10 / ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) e [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [PostgreSQL 17](https://www.postgresql.org/docs/17/)
- [xUnit](https://xunit.net/), [WebApplicationFactory](https://learn.microsoft.com/aspnet/core/test/integration-tests) e [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- Docker
