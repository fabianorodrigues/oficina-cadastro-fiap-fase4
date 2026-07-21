namespace Oficina.Cadastro.Domain.Clientes.ValueObjects;

public sealed class DocumentoCpfCnpj
{
    private DocumentoCpfCnpj() { }

    public DocumentoCpfCnpj(string valor)
    {
        var normalizado = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalizado.Length is not (11 or 14))
            throw new ArgumentException("Documento deve conter CPF ou CNPJ valido.", nameof(valor));

        Valor = normalizado;
    }

    public string Valor { get; private set; } = string.Empty;
}
