namespace Oficina.Cadastro.Domain.Clientes.ValueObjects;

public sealed class Contato
{
    private Contato() { }

    public Contato(string email, string telefone)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Email invalido.", nameof(email));

        var telefoneNormalizado = new string((telefone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (telefoneNormalizado.Length < 10)
            throw new ArgumentException("Telefone invalido.", nameof(telefone));

        Email = email.Trim();
        Telefone = telefoneNormalizado;
    }

    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
}
