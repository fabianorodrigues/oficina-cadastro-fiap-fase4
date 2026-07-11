using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.DTO.Veiculos;
using Oficina.Cadastro.Application.Shared;
using Oficina.Cadastro.Domain.Veiculos;
using Oficina.Cadastro.Domain.Veiculos.ValueObjects;

namespace Oficina.Cadastro.Application.UseCases.Veiculos;

public class CadastrarVeiculoUseCase(ICadastroRepository repo)
{
    public async Task<Guid> Executar(Guid clienteId, string placa, string renavam, ModeloRequest modelo, CancellationToken ct)
    {
        if (await repo.ObterCliente(clienteId, ct) is null)
            throw new OficinaException("Cliente nao encontrado.", 404);

        var placaVo = new Placa(placa);
        var renavamVo = new Renavam(renavam);

        if (await repo.ExisteVeiculoPorPlaca(placaVo.Valor, ct))
            throw new OficinaException("Ja existe veiculo cadastrado com esta placa.", 409);
        if (await repo.ExisteVeiculoPorRenavam(renavamVo.Valor, ct))
            throw new OficinaException("Ja existe veiculo cadastrado com este RENAVAM.", 409);

        var veiculo = new Veiculo(
            clienteId,
            placaVo,
            renavamVo,
            new Modelo(modelo.Descricao, modelo.Marca, modelo.Ano));

        await repo.AdicionarVeiculo(veiculo, ct);
        await repo.Salvar(ct);
        return veiculo.Id;
    }
}

public class AtualizarVeiculoUseCase(ICadastroRepository repo)
{
    public async Task Executar(Guid id, string placa, string renavam, ModeloRequest modelo, CancellationToken ct)
    {
        var veiculo = await repo.ObterVeiculo(id, ct)
            ?? throw new OficinaException("Veiculo nao encontrado.", 404);

        var placaVo = new Placa(placa);
        var renavamVo = new Renavam(renavam);

        if (placaVo.Valor != veiculo.Placa.Valor && await repo.ExisteVeiculoPorPlaca(placaVo.Valor, ct))
            throw new OficinaException("Ja existe veiculo cadastrado com esta placa.", 409);
        if (renavamVo.Valor != veiculo.Renavam.Valor && await repo.ExisteVeiculoPorRenavam(renavamVo.Valor, ct))
            throw new OficinaException("Ja existe veiculo cadastrado com este RENAVAM.", 409);

        veiculo.AlterarPlaca(placaVo);
        veiculo.AlterarRenavam(renavamVo);
        veiculo.AtualizarModelo(new Modelo(modelo.Descricao, modelo.Marca, modelo.Ano));
        await repo.Salvar(ct);
    }
}

public class ListarVeiculosUseCase(ICadastroRepository repo)
{
    public Task<IReadOnlyList<Veiculo>> Executar(CancellationToken ct) => repo.ListarVeiculos(ct);
}

public class ObterVeiculoUseCase(ICadastroRepository repo)
{
    public async Task<Veiculo> Executar(Guid id, CancellationToken ct)
        => await repo.ObterVeiculo(id, ct) ?? throw new OficinaException("Veiculo nao encontrado.", 404);
}

public class ListarVeiculosPorClienteUseCase(ICadastroRepository repo)
{
    public Task<IReadOnlyList<Veiculo>> Executar(Guid clienteId, CancellationToken ct)
        => repo.ListarVeiculosPorCliente(clienteId, ct);
}
