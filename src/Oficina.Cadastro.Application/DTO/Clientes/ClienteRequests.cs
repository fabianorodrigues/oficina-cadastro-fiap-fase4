namespace Oficina.Cadastro.Application.DTO.Clientes;

public record CadastrarClienteRequest(string CpfCnpj, string Nome, string Email, string Telefone);
public record AtualizarClienteRequest(string CpfCnpj, string Nome, string Email, string Telefone);
