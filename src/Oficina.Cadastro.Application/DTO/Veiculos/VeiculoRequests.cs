namespace Oficina.Cadastro.Application.DTO.Veiculos;

public record ModeloRequest(string Descricao, string Marca, int Ano);
public record CadastrarVeiculoRequest(Guid ClienteId, string Placa, string Renavam, ModeloRequest Modelo);
public record AtualizarVeiculoRequest(string Placa, string Renavam, ModeloRequest Modelo);
