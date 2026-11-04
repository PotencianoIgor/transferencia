namespace Transferencias.Domain;

public interface ITransacao : IAsyncDisposable
{
    Task ConfirmarAsync();
}
