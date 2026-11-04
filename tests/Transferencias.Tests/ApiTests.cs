using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Transferencias.Domain;
using Transferencias.Infrastructure;

namespace Transferencias.Tests;

[Collection(nameof(ApiCollection))]
public class ApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.RecriarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Transferencia_valida_retorna_201_e_atualiza_saldos_e_movimentacoes()
    {
        var resposta = await _client.TransferirAsync("A", "B", 30000);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var transferencia = await resposta.Content.ReadFromJsonAsync<TransferenciaResponse>();
        Assert.Equal(new TransferenciaResponse(transferencia!.Id, "A", "B", 30000), transferencia);
        Assert.Equal(70000, await _client.SaldoAsync("A"));
        Assert.Equal(80000, await _client.SaldoAsync("B"));
        Assert.Equal([transferencia], await _client.MovimentacoesAsync("A"));
        Assert.Equal([transferencia], await _client.MovimentacoesAsync("B"));
    }

    [Theory]
    [InlineData("A", "B", 0, HttpStatusCode.BadRequest)]
    [InlineData("A", "X", 100, HttpStatusCode.NotFound)]
    [InlineData("A", "B", 100001, HttpStatusCode.Conflict)]
    public async Task Transferencia_recusada_retorna_status_e_nao_altera_saldos(string origem, string destino, long valorCentavos, HttpStatusCode statusEsperado)
    {
        var resposta = await _client.TransferirAsync(origem, destino, valorCentavos);

        Assert.Equal(statusEsperado, resposta.StatusCode);
        await AssertNadaMudouAsync();
    }

    [Fact]
    public async Task Conta_inexistente_retorna_404()
    {
        var resposta = await _client.GetAsync("/contas/X");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Falha_antes_de_confirmar_nao_deixa_transferencia_parcial()
    {
        var clientComFalha = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IContaRepository>(provider =>
                new ContaRepositoryQueFalhaAoConfirmar(ActivatorUtilities.CreateInstance<ContaRepository>(provider)))))
            .CreateClient();

        var resposta = await clientComFalha.TransferirAsync("A", "B", 30000);

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        await AssertNadaMudouAsync();
    }

    private async Task AssertNadaMudouAsync()
    {
        Assert.Equal(100000, await _client.SaldoAsync("A"));
        Assert.Equal(50000, await _client.SaldoAsync("B"));
        Assert.Empty(await _client.MovimentacoesAsync("A"));
    }

    private class ContaRepositoryQueFalhaAoConfirmar(IContaRepository repository) : IContaRepository
    {
        public async Task<ITransacao> IniciarTransacaoAsync() =>
            new TransacaoQueFalhaAoConfirmar(await repository.IniciarTransacaoAsync());

        public Task<IReadOnlyList<Conta>> ObterParaAtualizacaoAsync(string origem, string destino) =>
            repository.ObterParaAtualizacaoAsync(origem, destino);

        public Task<Conta?> ObterAsync(string id) => repository.ObterAsync(id);

        public Task<IReadOnlyList<Transferencia>> ListarMovimentacoesAsync(string contaId) =>
            repository.ListarMovimentacoesAsync(contaId);

        public void Adicionar(Transferencia transferencia) => repository.Adicionar(transferencia);

        public Task SalvarAsync() => repository.SalvarAsync();

        private class TransacaoQueFalhaAoConfirmar(ITransacao transacao) : ITransacao
        {
            public Task ConfirmarAsync() => throw new InvalidOperationException("Falha simulada antes do commit.");

            public ValueTask DisposeAsync() => transacao.DisposeAsync();
        }
    }
}
