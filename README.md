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
    ConcorrenciaTests            Questão 2: duas transferências simultâneas sem saldo para ambas
```

## Questão 2: concorrência

**Cenário:** a conta tem 5000 centavos e recebe duas transferências simultâneas de 4000. As duas eram concluídas.

**Causa:** a transferência lê o saldo, valida e depois grava (*check-then-act*), sem nada que impeça outra transação de ler o mesmo saldo nesse intervalo. As duas requisições leem 5000, as duas passam na validação, e as duas gravam saldo 1000 e registram a transferência. Uma das atualizações se perde (*lost update*) e a conta entrega 8000 tendo só 5000.

**Correção:** dentro da transação, as contas são lidas com `SELECT ... FOR UPDATE` (`ContaRepository.ObterParaAtualizacaoAsync`). A segunda transação espera a primeira terminar e então lê o saldo já atualizado (1000), e a validação recusa com 409. As contas são travadas em ordem (`ORDER BY id`) para que transferências cruzadas (A→B e B→A) não entrem em deadlock.

**Teste:** `ConcorrenciaTests` prepara uma conta com 5000 centavos, dispara as duas requisições ao mesmo tempo e verifica:

- exatamente uma resposta 201 e uma 409
- saldo final da origem igual a 1000 (nunca negativo)
- destino creditado uma única vez
- uma única movimentação registrada

O histórico do git mostra o teste falhando antes da correção (commit "Questão 2: teste que reproduz...") e passando depois.

## Decisões

- **Valores em centavos (`long`)**, sem ponto flutuante.
- **Regras no domínio:** `Conta.Debitar` recusa saldo insuficiente e `TransferenciaService` valida a entrada e orquestra a operação.
- **Atomicidade:** débito, crédito e registro acontecem numa única transação. Se qualquer passo falhar antes do commit, a transação é desfeita. O teste `Falha_antes_de_confirmar_nao_deixa_transferencia_parcial` substitui o repositório por um que falha na confirmação e verifica que saldos e movimentações não mudaram.
- **Substituição de dependências:** o domínio depende de `IContaRepository` e `ITransacao`, sem conhecer o EF Core. Os testes de regra usam um repositório fake.
- **Banco criado com `EnsureCreated`**, sem migrations, porque o escopo não exige evolução de esquema.

## Referências e ferramentas

- [.NET 10 / ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) e [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [PostgreSQL 18](https://www.postgresql.org/docs/18/)
- [xUnit](https://xunit.net/), [WebApplicationFactory](https://learn.microsoft.com/aspnet/core/test/integration-tests) e [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- Docker
