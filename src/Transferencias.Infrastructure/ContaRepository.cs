using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Transferencias.Domain;

namespace Transferencias.Infrastructure;

public class ContaRepository(AppDbContext context) : IContaRepository
{
    public async Task<ITransacao> IniciarTransacaoAsync() =>
        new Transacao(await context.Database.BeginTransactionAsync());

    public async Task<IReadOnlyList<Conta>> ObterParaAtualizacaoAsync(string origem, string destino) =>
        await context.Contas
            .FromSql($"SELECT * FROM contas WHERE id IN ({origem}, {destino}) ORDER BY id FOR UPDATE")
            .ToListAsync();

    public Task<Conta?> ObterAsync(string id) =>
        context.Contas.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Transferencia>> ListarMovimentacoesAsync(string contaId) =>
        await context.Transferencias
            .AsNoTracking()
            .Where(t => t.Origem == contaId || t.Destino == contaId)
            .OrderBy(t => t.Id)
            .ToListAsync();

    public void Adicionar(Transferencia transferencia) => context.Transferencias.Add(transferencia);

    public Task SalvarAsync() => context.SaveChangesAsync();

    private class Transacao(IDbContextTransaction transaction) : ITransacao
    {
        public Task ConfirmarAsync() => transaction.CommitAsync();

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
