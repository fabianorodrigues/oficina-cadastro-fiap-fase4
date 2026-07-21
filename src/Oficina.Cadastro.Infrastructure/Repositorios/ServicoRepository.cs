using Microsoft.EntityFrameworkCore;
using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Domain.CatalogoEstoque;
using Oficina.Cadastro.Infrastructure.Persistencia;

namespace Oficina.Cadastro.Infrastructure.Repositorios;

public class ServicoRepository(CadastroDbContext db) : IServicoRepository
{
    public async Task<IReadOnlyList<Servico>> ListarServicos(CancellationToken ct)
        => await Query().OrderBy(x => x.MaoDeObra).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Servico?> ObterServico(Guid id, CancellationToken ct)
        => Query().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Servico>> ObterServicosPorIds(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => await Query().Where(x => ids.Contains(x.Id)).ToListAsync(ct);

    public Task AdicionarServico(Servico servico, CancellationToken ct)
        => db.Servicos.AddAsync(servico, ct).AsTask();

    public Task Salvar(CancellationToken ct) => db.SaveChangesAsync(ct);

    private IQueryable<Servico> Query()
        => db.Servicos.Include(x => x.Pecas).Include(x => x.Insumos);
}
