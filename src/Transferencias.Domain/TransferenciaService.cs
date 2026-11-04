namespace Transferencias.Domain;

public class TransferenciaService(IContaRepository repository)
{
    public async Task<Transferencia> TransferirAsync(string origem, string destino, long valorCentavos)
    {
        if (string.IsNullOrWhiteSpace(origem) || string.IsNullOrWhiteSpace(destino))
            throw new EntradaInvalidaException("Origem e destino são obrigatórios.");

        if (origem == destino)
            throw new EntradaInvalidaException("Origem e destino devem ser diferentes.");

        if (valorCentavos <= 0)
            throw new EntradaInvalidaException("O valor deve ser maior que zero.");

        await using var transacao = await repository.IniciarTransacaoAsync();

        var contas = await repository.ObterParaAtualizacaoAsync(origem, destino);
        var contaOrigem = contas.SingleOrDefault(c => c.Id == origem) ?? throw new ContaNaoEncontradaException(origem);
        var contaDestino = contas.SingleOrDefault(c => c.Id == destino) ?? throw new ContaNaoEncontradaException(destino);

        contaOrigem.Debitar(valorCentavos);
        contaDestino.Creditar(valorCentavos);

        var transferencia = new Transferencia(origem, destino, valorCentavos);
        repository.Adicionar(transferencia);

        await repository.SalvarAsync();
        await transacao.ConfirmarAsync();

        return transferencia;
    }

    public async Task<Conta> ObterContaAsync(string id) =>
        await repository.ObterAsync(id) ?? throw new ContaNaoEncontradaException(id);

    public async Task<IReadOnlyList<Transferencia>> ListarMovimentacoesAsync(string contaId)
    {
        await ObterContaAsync(contaId);
        return await repository.ListarMovimentacoesAsync(contaId);
    }
}
