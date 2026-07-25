using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.Abstractions.Seguranca;
using Oficina.Cadastro.Domain.CatalogoEstoque;
using Oficina.Cadastro.Domain.Clientes;
using Oficina.Cadastro.Domain.Seguranca;
using Oficina.Cadastro.Domain.Veiculos;

namespace Oficina.Cadastro.UnitTests.Fakes;

/// <summary>
/// Repositorios em memoria escritos a mao. Substituem apenas a persistencia:
/// as regras exercitadas continuam sendo as dos use cases e do dominio reais.
/// </summary>
public sealed class CadastroRepositoryEmMemoria : ICadastroRepository
{
    private readonly List<Cliente> _clientes = [];
    private readonly List<Veiculo> _veiculos = [];

    public int ChamadasSalvar { get; private set; }

    public Task<IReadOnlyList<Cliente>> ListarClientes(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Cliente>>(_clientes.ToList());

    public Task<Cliente?> ObterCliente(Guid id, CancellationToken ct)
        => Task.FromResult(_clientes.FirstOrDefault(x => x.Id == id));

    public Task<bool> ExisteClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct)
        => Task.FromResult(_clientes.Any(x => x.Documento.Valor == cpfCnpjNormalizado));

    public Task<Cliente?> ObterClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct)
        => Task.FromResult(_clientes.FirstOrDefault(x => x.Documento.Valor == cpfCnpjNormalizado));

    public Task AdicionarCliente(Cliente cliente, CancellationToken ct)
    {
        _clientes.Add(cliente);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Veiculo>> ListarVeiculos(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Veiculo>>(_veiculos.ToList());

    public Task<Veiculo?> ObterVeiculo(Guid id, CancellationToken ct)
        => Task.FromResult(_veiculos.FirstOrDefault(x => x.Id == id));

    public Task<Veiculo?> ObterVeiculoPorPlaca(string placaNormalizada, CancellationToken ct)
        => Task.FromResult(_veiculos.FirstOrDefault(x => x.Placa.Valor == placaNormalizada));

    public Task<IReadOnlyList<Veiculo>> ListarVeiculosPorCliente(Guid clienteId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Veiculo>>(_veiculos.Where(x => x.ClienteId == clienteId).ToList());

    public Task<bool> ExisteVeiculoPorPlaca(string placaNormalizada, CancellationToken ct)
        => Task.FromResult(_veiculos.Any(x => x.Placa.Valor == placaNormalizada));

    public Task<bool> ExisteVeiculoPorRenavam(string renavamNormalizado, CancellationToken ct)
        => Task.FromResult(_veiculos.Any(x => x.Renavam.Valor == renavamNormalizado));

    public Task AdicionarVeiculo(Veiculo veiculo, CancellationToken ct)
    {
        _veiculos.Add(veiculo);
        return Task.CompletedTask;
    }

    public Task Salvar(CancellationToken ct)
    {
        ChamadasSalvar++;
        return Task.CompletedTask;
    }
}

public sealed class ServicoRepositoryEmMemoria : IServicoRepository
{
    private readonly List<Servico> _servicos = [];

    public Task<IReadOnlyList<Servico>> ListarServicos(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Servico>>(_servicos.ToList());

    public Task<Servico?> ObterServico(Guid id, CancellationToken ct)
        => Task.FromResult(_servicos.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<Servico>> ObterServicosPorIds(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Servico>>(_servicos.Where(x => ids.Contains(x.Id)).ToList());

    public Task AdicionarServico(Servico servico, CancellationToken ct)
    {
        _servicos.Add(servico);
        return Task.CompletedTask;
    }

    public Task Salvar(CancellationToken ct) => Task.CompletedTask;
}

public sealed class FuncionarioRepositoryEmMemoria : IFuncionarioRepository
{
    private readonly List<Funcionario> _funcionarios = [];

    public Task<Funcionario?> ObterPorId(Guid id, CancellationToken ct)
        => Task.FromResult(_funcionarios.FirstOrDefault(x => x.Id == id));

    public Task<Funcionario?> ObterPorCpf(string cpfNormalizado, CancellationToken ct)
        => Task.FromResult(_funcionarios.FirstOrDefault(x => x.Cpf == cpfNormalizado));

    public Task<bool> ExistePorCpf(string cpfNormalizado, CancellationToken ct)
        => Task.FromResult(_funcionarios.Any(x => x.Cpf == cpfNormalizado));

    public Task<IReadOnlyList<Funcionario>> Listar(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Funcionario>>(_funcionarios.ToList());

    public Task Adicionar(Funcionario funcionario, CancellationToken ct)
    {
        _funcionarios.Add(funcionario);
        return Task.CompletedTask;
    }

    public Task Salvar(CancellationToken ct) => Task.CompletedTask;
}

/// <summary>
/// Hash reversivel e deliberado: o objetivo aqui e provar o fluxo dos use cases,
/// nao a criptografia, que tem testes proprios em PasswordHashServiceTests.
/// </summary>
public sealed class PasswordHashServiceFake : IPasswordHashService
{
    public string Hash(string senha) => $"hash::{senha}";

    public bool Verificar(string senhaHash, string senha) => senhaHash == $"hash::{senha}";
}
