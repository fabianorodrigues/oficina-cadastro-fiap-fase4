using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.DTO.Internal;
using Oficina.Cadastro.Application.Shared;

namespace Oficina.Cadastro.Application.UseCases.Internal;

public class ObterClienteInternalUseCase(ICadastroRepository repo)
{
    public async Task<ClienteInternalResponse> Executar(Guid id, CancellationToken ct)
    {
        var cliente = await repo.ObterCliente(id, ct) ?? throw new OficinaException("Cliente nao encontrado.", 404);
        return new ClienteInternalResponse
        {
            Id = cliente.Id,
            Documento = cliente.Documento.Valor,
            Nome = cliente.Nome,
            Email = cliente.Contato.Email,
            Telefone = cliente.Contato.Telefone
        };
    }
}

public class ObterClientePorDocumentoInternalUseCase(ICadastroRepository repo)
{
    public async Task<ClienteInternalResponse> Executar(string documento, CancellationToken ct)
    {
        var normalizado = new string(documento.Where(char.IsDigit).ToArray());
        var cliente = await repo.ObterClientePorDocumento(normalizado, ct) ?? throw new OficinaException("Cliente nao encontrado.", 404);
        return new ClienteInternalResponse
        {
            Id = cliente.Id,
            Documento = cliente.Documento.Valor,
            Nome = cliente.Nome,
            Email = cliente.Contato.Email,
            Telefone = cliente.Contato.Telefone
        };
    }
}

public class ObterVeiculoInternalUseCase(ICadastroRepository repo)
{
    public async Task<VeiculoInternalResponse> Executar(Guid id, CancellationToken ct)
    {
        var veiculo = await repo.ObterVeiculo(id, ct) ?? throw new OficinaException("Veiculo nao encontrado.", 404);
        return new VeiculoInternalResponse
        {
            Id = veiculo.Id,
            ClienteId = veiculo.ClienteId,
            Placa = veiculo.Placa.Valor,
            Renavam = veiculo.Renavam.Valor,
            ModeloDescricao = veiculo.Modelo.Descricao,
            ModeloMarca = veiculo.Modelo.Marca,
            ModeloAno = veiculo.Modelo.Ano
        };
    }
}

public class ObterVeiculoPorPlacaInternalUseCase(ICadastroRepository repo)
{
    public async Task<VeiculoInternalResponse> Executar(string placa, CancellationToken ct)
    {
        var normalizada = new string(placa.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var veiculo = await repo.ObterVeiculoPorPlaca(normalizada, ct) ?? throw new OficinaException("Veiculo nao encontrado.", 404);
        return new VeiculoInternalResponse
        {
            Id = veiculo.Id,
            ClienteId = veiculo.ClienteId,
            Placa = veiculo.Placa.Valor,
            Renavam = veiculo.Renavam.Valor,
            ModeloDescricao = veiculo.Modelo.Descricao,
            ModeloMarca = veiculo.Modelo.Marca,
            ModeloAno = veiculo.Modelo.Ano
        };
    }
}

public class ConsultarServicosInternalUseCase(IServicoRepository repo)
{
    public async Task<ConsultarServicosResponse> Executar(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var normalizados = ids.Distinct().ToArray();
        var encontrados = await repo.ObterServicosPorIds(normalizados, ct);
        var encontradosIds = encontrados.Select(x => x.Id).ToHashSet();

        return new ConsultarServicosResponse
        {
            Encontrados = encontrados.Select(servico => new ServicoInternalResponse
            {
                Id = servico.Id,
                MaoDeObra = servico.MaoDeObra,
                Pecas = servico.Pecas.Select(x => new ReferenciaMaterialInternalResponse
                {
                    ReferenciaId = x.PecaId,
                    Quantidade = x.Quantidade
                }).ToList(),
                Insumos = servico.Insumos.Select(x => new ReferenciaMaterialInternalResponse
                {
                    ReferenciaId = x.InsumoId,
                    Quantidade = x.Quantidade
                }).ToList()
            }).ToList(),
            Ausentes = normalizados.Where(id => !encontradosIds.Contains(id)).ToList()
        };
    }
}
