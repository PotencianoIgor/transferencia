namespace Transferencias.Domain;

public class EntradaInvalidaException(string mensagem) : Exception(mensagem);

public class ContaNaoEncontradaException(string contaId) : Exception($"Conta {contaId} não encontrada.");

public class SaldoInsuficienteException(string contaId) : Exception($"Saldo insuficiente na conta {contaId}.");
