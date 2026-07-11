using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Oficina.Cadastro.Api.Configuration;
using Oficina.Cadastro.Api.Controllers;
using Oficina.Cadastro.Api.Security;
using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.Abstractions.Seguranca;
using Oficina.Cadastro.Application.DTO.CatalogoEstoque;
using Oficina.Cadastro.Application.DTO.Clientes;
using Oficina.Cadastro.Application.DTO.Seguranca;
using Oficina.Cadastro.Application.DTO.Veiculos;
using Oficina.Cadastro.Application.Shared;
using Oficina.Cadastro.Application.UseCases.CatalogoEstoque;
using Oficina.Cadastro.Application.UseCases.Clientes;
using Oficina.Cadastro.Application.UseCases.Internal;
using Oficina.Cadastro.Application.UseCases.Seguranca;
using Oficina.Cadastro.Application.UseCases.Veiculos;
using Oficina.Cadastro.Application.Validators.CatalogoEstoque;
using Oficina.Cadastro.Application.Validators.Clientes;
using Oficina.Cadastro.Application.Validators.Veiculos;
using Oficina.Cadastro.Domain.CatalogoEstoque;
using Oficina.Cadastro.Domain.Clientes;
using Oficina.Cadastro.Domain.Clientes.ValueObjects;
using Oficina.Cadastro.Domain.Seguranca;
using Oficina.Cadastro.Domain.Seguranca.Enums;
using Oficina.Cadastro.Domain.Veiculos;
using Oficina.Cadastro.Domain.Veiculos.ValueObjects;
using Oficina.Cadastro.Infrastructure;

namespace Oficina.Cadastro.UnitTests;

public class ApplicationAndDomainTests
{
    [Fact]
    public void ValueObjects_devem_normalizar_dados()
    {
        Assert.Equal("12345678909", new DocumentoCpfCnpj("123.456.789-09").Valor);
        Assert.Equal("ABC1D23", new Placa("abc-1d23").Valor);
        Assert.Equal("12345678901", new Renavam("12345678901").Valor);
        Assert.Equal("11999999999", new Contato("a@b.com", "(11)99999-9999").Telefone);
    }

    [Fact]
    public void Servico_deve_manter_receitas_opacas()
    {
        var pecaId = Guid.NewGuid();
        var insumoId = Guid.NewGuid();
        var servico = new Servico(120m);

        servico.AdicionarPeca(pecaId, 2);
        servico.AdicionarInsumo(insumoId, 3);

        Assert.Equal(120m, servico.MaoDeObra);
        Assert.Equal(pecaId, Assert.Single(servico.Pecas).PecaId);
        Assert.Equal(insumoId, Assert.Single(servico.Insumos).InsumoId);
    }

    [Fact]
    public async Task CadastrarCliente_deve_adicionar_e_salvar()
    {
        var repo = new FakeCadastroRepository();
        var useCase = new CadastrarClienteUseCase(repo);

        var id = await useCase.Executar("12345678909", "Maria", "maria@email.com", "11999999999", CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.True(repo.Salvou);
        Assert.Single(repo.Clientes);
    }

    [Fact]
    public async Task CadastrarVeiculo_deve_bloquear_renavam_duplicado()
    {
        var repo = new FakeCadastroRepository();
        var cliente = new Cliente(new DocumentoCpfCnpj("12345678909"), "Maria", new Contato("maria@email.com", "11999999999"));
        repo.Clientes.Add(cliente);
        repo.Veiculos.Add(new Veiculo(cliente.Id, new Placa("ABC1D23"), new Renavam("12345678901"), new Modelo("Uno", "Fiat", 2020)));
        var useCase = new CadastrarVeiculoUseCase(repo);

        var ex = await Assert.ThrowsAsync<OficinaException>(() => useCase.Executar(
            cliente.Id,
            "DEF4G56",
            "12345678901",
            new ModeloRequest("Onix", "Chevrolet", 2022),
            CancellationToken.None));

        Assert.Equal(409, ex.StatusHttp);
    }

    [Fact]
    public async Task AtualizarVeiculo_deve_preservar_ownership_do_cliente()
    {
        var repo = new FakeCadastroRepository();
        var clienteId = Guid.NewGuid();
        var veiculo = new Veiculo(clienteId, new Placa("ABC1D23"), new Renavam("12345678901"), new Modelo("Uno", "Fiat", 2020));
        repo.Veiculos.Add(veiculo);

        await new AtualizarVeiculoUseCase(repo).Executar(
            veiculo.Id,
            "ABC1D23",
            "12345678901",
            new ModeloRequest("Uno", "Fiat", 2021),
            CancellationToken.None);

        Assert.Equal(clienteId, veiculo.ClienteId);
        Assert.Equal(2021, veiculo.Modelo.Ano);
    }

    [Fact]
    public async Task Consulta_batch_de_servicos_deve_retornar_encontrados_e_ausentes()
    {
        var repo = new FakeServicoRepository();
        var servico = new Servico(250m);
        var pecaId = Guid.NewGuid();
        servico.AdicionarPeca(pecaId, 1);
        repo.Servicos.Add(servico);
        var ausente = Guid.NewGuid();

        var response = await new ConsultarServicosInternalUseCase(repo).Executar([servico.Id, ausente], CancellationToken.None);

        var encontrado = Assert.Single(response.Encontrados);
        Assert.Equal(servico.Id, encontrado.Id);
        Assert.Equal(250m, encontrado.MaoDeObra);
        Assert.Equal(pecaId, Assert.Single(encontrado.Pecas).ReferenciaId);
        Assert.Equal(ausente, Assert.Single(response.Ausentes));
    }

    [Fact]
    public async Task Funcionario_deve_guardar_senha_em_hash()
    {
        var repo = new FakeFuncionarioRepository();
        var password = new FakePasswordHashService();

        var response = await new CriarFuncionarioUseCase(repo, password).Executar(
            new CriarFuncionarioRequest("Ana", "12345678909", "Senha!123", "Admin"),
            CancellationToken.None);

        var funcionario = Assert.Single(repo.Funcionarios);
        Assert.Equal("HASH:Senha!123", funcionario.SenhaHash);
        Assert.Equal("Admin", response.Perfil);
    }

    [Fact]
    public void Validators_devem_rejeitar_payloads_invalidos()
    {
        Assert.False(new CadastrarClienteRequestValidator()
            .Validate(new CadastrarClienteRequest("", "", "x", "1")).IsValid);
        Assert.False(new CadastrarVeiculoRequestValidator()
            .Validate(new CadastrarVeiculoRequest(Guid.Empty, "", "1", new ModeloRequest("", "", 1800))).IsValid);
        Assert.False(new CadastrarServicoRequestValidator()
            .Validate(new CadastrarServicoRequest(-1, [new ItemRequeridoRequest(Guid.Empty, 0)], null)).IsValid);
    }

    [Fact]
    public void Controllers_devem_preservar_policies_publicas()
    {
        AssertAuthorizePolicy<ClientesController>(Policies.FuncionarioOuAdmin);
        AssertAuthorizePolicy<VeiculosController>(Policies.FuncionarioOuAdmin);
        AssertAuthorizePolicy<ServicosController>(Policies.FuncionarioOuAdmin);
        AssertAuthorizePolicy<AdminFuncionariosController>(Policies.AdminOnly);
        AssertAuthorizePolicy<InternalController>(Policies.FuncionarioOuAdmin);
    }

    [Fact]
    public void Development_auth_deve_falhar_fora_de_development()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Mode"] = "Development" })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddDevelopmentAuthentication(config, new FakeEnvironment("Production")));

        Assert.Contains("Development", ex.Message);
    }

    [Fact]
    public void Development_deve_permitir_configuracao_local_sem_connection_string_explicita()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddCadastroInfrastructure(config, allowLocalFallback: true);

        Assert.Contains(services, service => service.ServiceType.Name.Contains("DbContextOptions"));
    }

    [Fact]
    public void Production_deve_rejeitar_authentication_mode_development()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "Development",
                ["ConnectionStrings:OficinaCadastroDb"] = "Server=.;Database=OficinaCadastroDb;Trusted_Connection=True"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            config.ValidateCadastroProductionConfiguration(new FakeEnvironment("Production")));

        Assert.Equal("Modo de autenticacao Development nao pode ser utilizado em Production.", ex.Message);
    }

    [Fact]
    public void Production_deve_rejeitar_connection_string_ausente_com_mensagem_clara()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Mode"] = "Jwt" })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            config.ValidateCadastroProductionConfiguration(new FakeEnvironment("Production")));

        Assert.Equal("Connection string obrigatoria nao foi configurada.", ex.Message);
    }

    [Fact]
    public void Production_nao_deve_usar_fallback_local_de_banco()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddCadastroInfrastructure(config, allowLocalFallback: false));

        Assert.Equal("Connection string obrigatoria nao foi configurada.", ex.Message);
    }

    private static void AssertAuthorizePolicy<T>(string policy)
    {
        var attribute = typeof(T).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Single();
        Assert.Equal(policy, attribute.Policy);
    }

    private sealed class FakeCadastroRepository : ICadastroRepository
    {
        public List<Cliente> Clientes { get; } = [];
        public List<Veiculo> Veiculos { get; } = [];
        public bool Salvou { get; private set; }
        public Task<IReadOnlyList<Cliente>> ListarClientes(CancellationToken ct) => Task.FromResult<IReadOnlyList<Cliente>>(Clientes);
        public Task<Cliente?> ObterCliente(Guid id, CancellationToken ct) => Task.FromResult(Clientes.FirstOrDefault(x => x.Id == id));
        public Task<bool> ExisteClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct) => Task.FromResult(Clientes.Any(x => x.Documento.Valor == cpfCnpjNormalizado));
        public Task<Cliente?> ObterClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct) => Task.FromResult(Clientes.FirstOrDefault(x => x.Documento.Valor == cpfCnpjNormalizado));
        public Task AdicionarCliente(Cliente cliente, CancellationToken ct) { Clientes.Add(cliente); return Task.CompletedTask; }
        public Task<IReadOnlyList<Veiculo>> ListarVeiculos(CancellationToken ct) => Task.FromResult<IReadOnlyList<Veiculo>>(Veiculos);
        public Task<Veiculo?> ObterVeiculo(Guid id, CancellationToken ct) => Task.FromResult(Veiculos.FirstOrDefault(x => x.Id == id));
        public Task<Veiculo?> ObterVeiculoPorPlaca(string placaNormalizada, CancellationToken ct) => Task.FromResult(Veiculos.FirstOrDefault(x => x.Placa.Valor == placaNormalizada));
        public Task<IReadOnlyList<Veiculo>> ListarVeiculosPorCliente(Guid clienteId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Veiculo>>(Veiculos.Where(x => x.ClienteId == clienteId).ToList());
        public Task<bool> ExisteVeiculoPorPlaca(string placaNormalizada, CancellationToken ct) => Task.FromResult(Veiculos.Any(x => x.Placa.Valor == placaNormalizada));
        public Task<bool> ExisteVeiculoPorRenavam(string renavamNormalizado, CancellationToken ct) => Task.FromResult(Veiculos.Any(x => x.Renavam.Valor == renavamNormalizado));
        public Task AdicionarVeiculo(Veiculo veiculo, CancellationToken ct) { Veiculos.Add(veiculo); return Task.CompletedTask; }
        public Task Salvar(CancellationToken ct) { Salvou = true; return Task.CompletedTask; }
    }

    private sealed class FakeServicoRepository : IServicoRepository
    {
        public List<Servico> Servicos { get; } = [];
        public Task<IReadOnlyList<Servico>> ListarServicos(CancellationToken ct) => Task.FromResult<IReadOnlyList<Servico>>(Servicos);
        public Task<Servico?> ObterServico(Guid id, CancellationToken ct) => Task.FromResult(Servicos.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Servico>> ObterServicosPorIds(IReadOnlyCollection<Guid> ids, CancellationToken ct) => Task.FromResult<IReadOnlyList<Servico>>(Servicos.Where(x => ids.Contains(x.Id)).ToList());
        public Task AdicionarServico(Servico servico, CancellationToken ct) { Servicos.Add(servico); return Task.CompletedTask; }
        public Task Salvar(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeFuncionarioRepository : IFuncionarioRepository
    {
        public List<Funcionario> Funcionarios { get; } = [];
        public Task<Funcionario?> ObterPorId(Guid id, CancellationToken ct) => Task.FromResult(Funcionarios.FirstOrDefault(x => x.Id == id));
        public Task<Funcionario?> ObterPorCpf(string cpfNormalizado, CancellationToken ct) => Task.FromResult(Funcionarios.FirstOrDefault(x => x.Cpf == cpfNormalizado));
        public Task<bool> ExistePorCpf(string cpfNormalizado, CancellationToken ct) => Task.FromResult(Funcionarios.Any(x => x.Cpf == cpfNormalizado));
        public Task<IReadOnlyList<Funcionario>> Listar(CancellationToken ct) => Task.FromResult<IReadOnlyList<Funcionario>>(Funcionarios);
        public Task Adicionar(Funcionario funcionario, CancellationToken ct) { Funcionarios.Add(funcionario); return Task.CompletedTask; }
        public Task Salvar(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string Hash(string senha) => $"HASH:{senha}";
        public bool Verificar(string senhaHash, string senha) => senhaHash == $"HASH:{senha}";
    }

    private sealed class FakeEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
