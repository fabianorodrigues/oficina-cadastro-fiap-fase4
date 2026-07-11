using Oficina.Cadastro.Domain.Clientes.ValueObjects;
using Oficina.Cadastro.Domain.Seguranca.Enums;
using Oficina.Cadastro.Domain.SharedKernel;

namespace Oficina.Cadastro.Domain.Seguranca;

public class Funcionario : AgregadoRaiz
{
    private Funcionario() { }

    public Funcionario(string nome, string cpf, string senhaHash, PerfilUsuarioInterno perfil)
    {
        Nome = NormalizarNome(nome);
        Cpf = NormalizarCpf(cpf);
        AlterarSenhaHash(senhaHash);
        Perfil = perfil;
        Ativo = true;
        DataCriacao = DateTimeOffset.UtcNow;
    }

    public string Nome { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public PerfilUsuarioInterno Perfil { get; private set; }
    public bool Ativo { get; private set; }
    public DateTimeOffset DataCriacao { get; private set; }

    public void Atualizar(string nome, PerfilUsuarioInterno perfil, bool ativo)
    {
        Nome = NormalizarNome(nome);
        Perfil = perfil;
        Ativo = ativo;
    }

    public void AlterarSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("Hash de senha e obrigatorio.", nameof(senhaHash));

        SenhaHash = senhaHash;
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;

    public static string NormalizarCpf(string cpf)
    {
        var normalizado = new DocumentoCpfCnpj(cpf).Valor;
        if (normalizado.Length != 11)
            throw new ArgumentException("CPF invalido.", nameof(cpf));

        return normalizado;
    }

    private static string NormalizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome e obrigatorio.", nameof(nome));

        return nome.Trim();
    }
}
