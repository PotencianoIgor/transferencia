using System.Net.Http.Json;

namespace Transferencias.Tests;

public record ContaResponse(string Id, long SaldoCentavos);

public record TransferenciaResponse(long Id, string Origem, string Destino, long ValorCentavos);

public static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> TransferirAsync(this HttpClient client, string origem, string destino, long valorCentavos) =>
        client.PostAsJsonAsync("/transferencias", new { origem, destino, valorCentavos });

    public static async Task<long> SaldoAsync(this HttpClient client, string contaId) =>
        (await client.GetFromJsonAsync<ContaResponse>($"/contas/{contaId}"))!.SaldoCentavos;

    public static async Task<List<TransferenciaResponse>> MovimentacoesAsync(this HttpClient client, string contaId) =>
        (await client.GetFromJsonAsync<List<TransferenciaResponse>>($"/contas/{contaId}/movimentacoes"))!;
}
