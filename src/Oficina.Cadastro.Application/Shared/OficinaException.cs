namespace Oficina.Cadastro.Application.Shared;

public class OficinaException(string mensagem, int statusHttp = 400) : Exception(mensagem)
{
    public int StatusHttp { get; } = statusHttp;
}
