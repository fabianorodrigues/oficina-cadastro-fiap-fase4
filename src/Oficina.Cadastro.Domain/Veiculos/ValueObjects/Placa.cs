namespace Oficina.Cadastro.Domain.Veiculos.ValueObjects;

public sealed class Placa
{
    private Placa() { }

    public Placa(string valor)
    {
        var normalizada = (valor ?? string.Empty).Replace("-", string.Empty).Trim().ToUpperInvariant();
        if (normalizada.Length != 7 || !normalizada.All(char.IsLetterOrDigit))
            throw new ArgumentException("Placa invalida.", nameof(valor));

        Valor = normalizada;
    }

    public string Valor { get; private set; } = string.Empty;
}
