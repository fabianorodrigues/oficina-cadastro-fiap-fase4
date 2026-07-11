namespace Oficina.Cadastro.Application.DTO.Internal;

public sealed class ClienteInternalResponse
{
    public Guid Id { get; init; }
    public string Documento { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Telefone { get; init; } = string.Empty;
}

public sealed class VeiculoInternalResponse
{
    public Guid Id { get; init; }
    public Guid ClienteId { get; init; }
    public string Placa { get; init; } = string.Empty;
    public string Renavam { get; init; } = string.Empty;
    public string ModeloDescricao { get; init; } = string.Empty;
    public string ModeloMarca { get; init; } = string.Empty;
    public int ModeloAno { get; init; }
}

public record ConsultarServicosRequest(IReadOnlyList<Guid> Ids);

public sealed class ConsultarServicosResponse
{
    public IReadOnlyList<ServicoInternalResponse> Encontrados { get; init; } = [];
    public IReadOnlyList<Guid> Ausentes { get; init; } = [];
}

public sealed class ServicoInternalResponse
{
    public Guid Id { get; init; }
    public decimal MaoDeObra { get; init; }
    public IReadOnlyList<ReferenciaMaterialInternalResponse> Pecas { get; init; } = [];
    public IReadOnlyList<ReferenciaMaterialInternalResponse> Insumos { get; init; } = [];
}

public sealed class ReferenciaMaterialInternalResponse
{
    public Guid ReferenciaId { get; init; }
    public int Quantidade { get; init; }
}
