using Oficina.Cadastro.Domain.Seguranca;

namespace Oficina.Cadastro.Application.Abstractions.Repositorios;

public interface IFuncionarioRepository
{
    Task<Funcionario?> ObterPorId(Guid id, CancellationToken ct);
    Task<Funcionario?> ObterPorCpf(string cpfNormalizado, CancellationToken ct);
    Task<bool> ExistePorCpf(string cpfNormalizado, CancellationToken ct);
    Task<IReadOnlyList<Funcionario>> Listar(CancellationToken ct);
    Task Adicionar(Funcionario funcionario, CancellationToken ct);
    Task Salvar(CancellationToken ct);
}
