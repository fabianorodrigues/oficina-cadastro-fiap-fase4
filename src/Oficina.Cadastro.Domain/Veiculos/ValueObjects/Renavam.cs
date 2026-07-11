namespace Oficina.Cadastro.Domain.Veiculos.ValueObjects;

public sealed class Renavam
{
    private Renavam() { }

    public Renavam(string valor)
    {
        var normalizado = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalizado.Length != 11)
            throw new ArgumentException("RENAVAM invalido.", nameof(valor));

        Valor = normalizado;
    }

    public string Valor { get; private set; } = string.Empty;
}
