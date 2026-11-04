namespace Transferencias.Domain;

public class Transferencia(string origem, string destino, long valorCentavos)
{
    public long Id { get; private set; }
    public string Origem { get; private set; } = origem;
    public string Destino { get; private set; } = destino;
    public long ValorCentavos { get; private set; } = valorCentavos;
}
