using Transferencias.Domain;

namespace Transferencias.Tests;

public class TransferenciaServiceTests
{
    private readonly Conta _contaA = new("A", 100000);
    private readonly Conta _contaB = new("B", 50000);
    private readonly ContaRepositoryFake _repository;
    private readonly TransferenciaService _service;

    public TransferenciaServiceTests()
    {
        _repository = new ContaRepositoryFake(_contaA, _contaB);
        _service = new TransferenciaService(_repository);
    }

    [Fact]
    public async Task Transferencia_debita_origem_credita_destino_e_registra_movimentacao()
    {
        await _service.TransferirAsync("A", "B", 30000);

        Assert.Equal(70000, _contaA.SaldoCentavos);
        Assert.Equal(80000, _contaB.SaldoCentavos);
        var transferencia = Assert.Single(_repository.Transferencias);
        Assert.Equal(("A", "B", 30000L), (transferencia.Origem, transferencia.Destino, transferencia.ValorCentavos));
    }

    [Theory]
    [InlineData("", "B", 100)]
    [InlineData("A", " ", 100)]
    [InlineData("A", "A", 100)]
    [InlineData("A", "B", 0)]
    [InlineData("A", "B", -100)]
    public async Task Entrada_invalida_e_recusada(string origem, string destino, long valorCentavos)
    {
        await Assert.ThrowsAsync<EntradaInvalidaException>(() => _service.TransferirAsync(origem, destino, valorCentavos));

        AssertNadaMudou();
    }

    [Theory]
    [InlineData("X", "B")]
    [InlineData("A", "X")]
    public async Task Conta_inexistente_e_recusada(string origem, string destino)
    {
        await Assert.ThrowsAsync<ContaNaoEncontradaException>(() => _service.TransferirAsync(origem, destino, 100));

        AssertNadaMudou();
    }

    [Fact]
    public async Task Saldo_insuficiente_e_recusado()
    {
        await Assert.ThrowsAsync<SaldoInsuficienteException>(() => _service.TransferirAsync("A", "B", 100001));

        AssertNadaMudou();
    }

    private void AssertNadaMudou()
    {
        Assert.Equal(100000, _contaA.SaldoCentavos);
        Assert.Equal(50000, _contaB.SaldoCentavos);
        Assert.Empty(_repository.Transferencias);
    }

    private class ContaRepositoryFake(params Conta[] contas) : IContaRepository
    {
        public List<Transferencia> Transferencias { get; } = [];

        public Task<ITransacao> IniciarTransacaoAsync() => Task.FromResult<ITransacao>(new TransacaoFake());

        public Task<IReadOnlyList<Conta>> ObterParaAtualizacaoAsync(string origem, string destino) =>
            Task.FromResult<IReadOnlyList<Conta>>(contas.Where(c => c.Id == origem || c.Id == destino).ToList());

        public Task<Conta?> ObterAsync(string id) => Task.FromResult(contas.SingleOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Transferencia>> ListarMovimentacoesAsync(string contaId) =>
            Task.FromResult<IReadOnlyList<Transferencia>>(Transferencias.Where(t => t.Origem == contaId || t.Destino == contaId).ToList());

        public void Adicionar(Transferencia transferencia) => Transferencias.Add(transferencia);

        public Task SalvarAsync() => Task.CompletedTask;

        private class TransacaoFake : ITransacao
        {
            public Task ConfirmarAsync() => Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
