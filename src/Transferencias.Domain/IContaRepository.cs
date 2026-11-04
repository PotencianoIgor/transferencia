namespace Transferencias.Domain;

public interface IContaRepository
{
    Task<ITransacao> IniciarTransacaoAsync();
    Task<IReadOnlyList<Conta>> ObterParaAtualizacaoAsync(string origem, string destino);
    Task<Conta?> ObterAsync(string id);
    Task<IReadOnlyList<Transferencia>> ListarMovimentacoesAsync(string contaId);
    void Adicionar(Transferencia transferencia);
    Task SalvarAsync();
}
