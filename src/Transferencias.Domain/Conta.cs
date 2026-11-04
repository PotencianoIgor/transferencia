namespace Transferencias.Domain;

public class Conta(string id, long saldoCentavos)
{
    public string Id { get; private set; } = id;
    public long SaldoCentavos { get; private set; } = saldoCentavos;

    public void Debitar(long valorCentavos)
    {
        if (valorCentavos > SaldoCentavos)
            throw new SaldoInsuficienteException(Id);

        SaldoCentavos -= valorCentavos;
    }

    public void Creditar(long valorCentavos) => SaldoCentavos += valorCentavos;
}
