namespace Oficina.Cadastro.Api.Contracts;

public record ModeloRequest(string Descricao, string Marca, int Ano);
public record CadastrarClienteRequest(string CpfCnpj, string Nome, string Email, string Telefone);
public record AtualizarClienteRequest(string CpfCnpj, string Nome, string Email, string Telefone);
public record CadastrarVeiculoRequest(Guid ClienteId, string Placa, string Renavam, ModeloRequest Modelo);
public record AtualizarVeiculoRequest(string Placa, string Renavam, ModeloRequest Modelo);

public record LoginCpfRequest(string Cpf, string? Senha);
public record CriarFuncionarioRequest(string Nome, string Cpf, string Senha, string Perfil);
public record AtualizarFuncionarioRequest(string Nome, string Perfil, bool Ativo);
public record AlterarSenhaFuncionarioRequest(string NovaSenha);
