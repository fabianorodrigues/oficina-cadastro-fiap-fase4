using Oficina.Cadastro.Domain.CatalogoEstoque;

namespace Oficina.Cadastro.Application.Abstractions.Repositorios;

public interface IServicoRepository
{
    Task<IReadOnlyList<Servico>> ListarServicos(CancellationToken ct);
    Task<Servico?> ObterServico(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Servico>> ObterServicosPorIds(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task AdicionarServico(Servico servico, CancellationToken ct);
    Task Salvar(CancellationToken ct);
}
