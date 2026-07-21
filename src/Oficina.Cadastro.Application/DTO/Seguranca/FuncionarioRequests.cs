namespace Oficina.Cadastro.Application.DTO.Seguranca;

public record CriarFuncionarioRequest(string Nome, string Cpf, string Senha, string Perfil);
public record AtualizarFuncionarioRequest(string Nome, string Perfil, bool Ativo);
public record AlterarSenhaFuncionarioRequest(string NovaSenha);

public sealed class FuncionarioResponse
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public string Perfil { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public DateTimeOffset DataCriacao { get; init; }
}
