using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Transferencias.Domain;
using Transferencias.Infrastructure;

namespace Transferencias.Tests;

[Collection(nameof(ApiCollection))]
public class ConcorrenciaTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.RecriarBancoAsync();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Contas.Add(new Conta("C", 5000));
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Duas_transferencias_simultaneas_sem_saldo_para_ambas_efetivam_somente_uma()
    {
        var respostas = await Task.WhenAll(
            _client.TransferirAsync("C", "B", 4000),
            _client.TransferirAsync("C", "B", 4000));

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], respostas.Select(r => r.StatusCode).Order());
        Assert.Equal(1000, await _client.SaldoAsync("C"));
        Assert.Equal(54000, await _client.SaldoAsync("B"));
        Assert.Single(await _client.MovimentacoesAsync("C"));
    }
}
