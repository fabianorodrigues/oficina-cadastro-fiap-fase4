using Oficina.Cadastro.Domain.Clientes.ValueObjects;
using Oficina.Cadastro.Domain.SharedKernel;

namespace Oficina.Cadastro.Domain.Clientes;

public class Cliente : AgregadoRaiz
{
    private Cliente() { }

    public Cliente(DocumentoCpfCnpj documento, string nome, Contato contato)
    {
        Documento = documento ?? throw new ArgumentNullException(nameof(documento));
        AlterarNome(nome);
        Contato = contato ?? throw new ArgumentNullException(nameof(contato));
    }

    public string Nome { get; private set; } = string.Empty;
    public DocumentoCpfCnpj Documento { get; private set; } = default!;
    public Contato Contato { get; private set; } = default!;

    public void AlterarDocumento(DocumentoCpfCnpj documento)
        => Documento = documento ?? throw new ArgumentNullException(nameof(documento));

    public void AlterarContato(Contato contato)
        => Contato = contato ?? throw new ArgumentNullException(nameof(contato));

    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome e obrigatorio.", nameof(nome));

        Nome = nome.Trim();
    }
}
