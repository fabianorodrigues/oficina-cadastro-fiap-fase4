namespace Oficina.Cadastro.Application.DTO.CatalogoEstoque;

public record CadastrarServicoRequest(
    decimal MaoDeObra,
    IReadOnlyList<ItemRequeridoRequest>? Pecas,
    IReadOnlyList<ItemRequeridoRequest>? Insumos);

public record ItemRequeridoRequest(Guid Id, int Quantidade);
