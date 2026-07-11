using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.Shared;
using Oficina.Cadastro.Domain.CatalogoEstoque;

namespace Oficina.Cadastro.Application.UseCases.CatalogoEstoque;

public class CadastrarServicoUseCase(IServicoRepository repo)
{
    public async Task<Guid> Executar(decimal maoDeObra, IEnumerable<(Guid id, int qtd)>? pecas, IEnumerable<(Guid id, int qtd)>? insumos, CancellationToken ct)
    {
        var servico = new Servico(maoDeObra);
        if (pecas is not null)
            foreach (var (id, qtd) in pecas)
                servico.AdicionarPeca(id, qtd);
        if (insumos is not null)
            foreach (var (id, qtd) in insumos)
                servico.AdicionarInsumo(id, qtd);

        await repo.AdicionarServico(servico, ct);
        await repo.Salvar(ct);
        return servico.Id;
    }
}

public class ListarServicosUseCase(IServicoRepository repo)
{
    public Task<IReadOnlyList<Servico>> Executar(CancellationToken ct) => repo.ListarServicos(ct);
}

public class ObterServicoUseCase(IServicoRepository repo)
{
    public async Task<Servico> Executar(Guid id, CancellationToken ct)
        => await repo.ObterServico(id, ct) ?? throw new OficinaException("Servico nao encontrado.", 404);
}

public class AtualizarServicoUseCase(IServicoRepository repo)
{
    public async Task Executar(decimal maoDeObra, Guid id, IEnumerable<(Guid id, int qtd)>? pecas, IEnumerable<(Guid id, int qtd)>? insumos, CancellationToken ct)
    {
        var servico = await repo.ObterServico(id, ct)
            ?? throw new OficinaException("Servico nao encontrado.", 404);

        servico.DefinirMaoDeObra(maoDeObra);
        servico.SubstituirPecas(pecas ?? []);
        servico.SubstituirInsumos(insumos ?? []);
        await repo.Salvar(ct);
    }
}
