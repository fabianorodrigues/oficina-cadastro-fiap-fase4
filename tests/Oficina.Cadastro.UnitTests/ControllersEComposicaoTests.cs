using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Cadastro.Api.Controllers;
using Oficina.Cadastro.Application;
using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.Abstractions.Seguranca;
using Oficina.Cadastro.Application.DTO.CatalogoEstoque;
using Oficina.Cadastro.Application.DTO.Clientes;
using Oficina.Cadastro.Application.DTO.Internal;
using Oficina.Cadastro.Application.DTO.Seguranca;
using Oficina.Cadastro.Application.DTO.Veiculos;
using Oficina.Cadastro.Application.Shared;
using Oficina.Cadastro.Application.UseCases.CatalogoEstoque;
using Oficina.Cadastro.Application.UseCases.Clientes;
using Oficina.Cadastro.Application.UseCases.Internal;
using Oficina.Cadastro.Application.UseCases.Seguranca;
using Oficina.Cadastro.Application.UseCases.Veiculos;
using Oficina.Cadastro.UnitTests.Fakes;

namespace Oficina.Cadastro.UnitTests;

/// <summary>
/// Container real da aplicacao com persistencia em memoria. Os controllers e os
/// use cases exercitados sao os de producao.
/// </summary>
public sealed class CadastroHost : IDisposable
{
    private readonly ServiceProvider _provider;

    public CadastroHost()
    {
        var services = new ServiceCollection();
        services.AddCadastroApplication();
        services.AddSingleton<ICadastroRepository, CadastroRepositoryEmMemoria>();
        services.AddSingleton<IServicoRepository, ServicoRepositoryEmMemoria>();
        services.AddSingleton<IFuncionarioRepository, FuncionarioRepositoryEmMemoria>();
        services.AddSingleton<IPasswordHashService, PasswordHashServiceFake>();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        Scope = _provider.CreateScope();
    }

    public IServiceScope Scope { get; }

    public T Resolver<T>() where T : notnull => Scope.ServiceProvider.GetRequiredService<T>();

    public ClientesController Clientes() => new(
        Resolver<CadastrarClienteUseCase>(), Resolver<AtualizarClienteUseCase>(),
        Resolver<ListarClientesUseCase>(), Resolver<ObterClienteUseCase>());

    public VeiculosController Veiculos() => new(
        Resolver<CadastrarVeiculoUseCase>(), Resolver<AtualizarVeiculoUseCase>(),
        Resolver<ListarVeiculosUseCase>(), Resolver<ObterVeiculoUseCase>());

    public ServicosController Servicos() => new(
        Resolver<CadastrarServicoUseCase>(), Resolver<ListarServicosUseCase>(),
        Resolver<ObterServicoUseCase>(), Resolver<AtualizarServicoUseCase>());

    public InternalController Internal() => new(
        Resolver<ObterClienteInternalUseCase>(), Resolver<ObterClientePorDocumentoInternalUseCase>(),
        Resolver<ObterVeiculoInternalUseCase>(), Resolver<ObterVeiculoPorPlacaInternalUseCase>(),
        Resolver<ConsultarServicosInternalUseCase>());

    public AdminFuncionariosController Funcionarios() => new(
        Resolver<CriarFuncionarioUseCase>(), Resolver<ListarFuncionariosUseCase>(),
        Resolver<ObterFuncionarioUseCase>(), Resolver<AtualizarFuncionarioUseCase>(),
        Resolver<AlterarSenhaFuncionarioUseCase>(), Resolver<AlterarStatusFuncionarioUseCase>());

    public void Dispose()
    {
        Scope.Dispose();
        _provider.Dispose();
    }
}

public class ComposicaoDaAplicacaoTests
{
    [Fact]
    public void Todos_os_use_cases_registrados_devem_ser_resolviveis()
    {
        // ValidateOnBuild ja reprova dependencia faltante; resolver cada um
        // confirma que o grafo tambem se constroi em tempo de execucao.
        using var host = new CadastroHost();

        Assert.NotNull(host.Resolver<CadastrarClienteUseCase>());
        Assert.NotNull(host.Resolver<AtualizarClienteUseCase>());
        Assert.NotNull(host.Resolver<ListarClientesUseCase>());
        Assert.NotNull(host.Resolver<ObterClienteUseCase>());
        Assert.NotNull(host.Resolver<CadastrarVeiculoUseCase>());
        Assert.NotNull(host.Resolver<AtualizarVeiculoUseCase>());
        Assert.NotNull(host.Resolver<ListarVeiculosUseCase>());
        Assert.NotNull(host.Resolver<ObterVeiculoUseCase>());
        Assert.NotNull(host.Resolver<ListarVeiculosPorClienteUseCase>());
        Assert.NotNull(host.Resolver<CadastrarServicoUseCase>());
        Assert.NotNull(host.Resolver<ListarServicosUseCase>());
        Assert.NotNull(host.Resolver<ObterServicoUseCase>());
        Assert.NotNull(host.Resolver<AtualizarServicoUseCase>());
        Assert.NotNull(host.Resolver<CriarFuncionarioUseCase>());
        Assert.NotNull(host.Resolver<ListarFuncionariosUseCase>());
        Assert.NotNull(host.Resolver<ObterFuncionarioUseCase>());
        Assert.NotNull(host.Resolver<AtualizarFuncionarioUseCase>());
        Assert.NotNull(host.Resolver<AlterarSenhaFuncionarioUseCase>());
        Assert.NotNull(host.Resolver<AlterarStatusFuncionarioUseCase>());
        Assert.NotNull(host.Resolver<ObterClienteInternalUseCase>());
        Assert.NotNull(host.Resolver<ObterClientePorDocumentoInternalUseCase>());
        Assert.NotNull(host.Resolver<ObterVeiculoInternalUseCase>());
        Assert.NotNull(host.Resolver<ObterVeiculoPorPlacaInternalUseCase>());
        Assert.NotNull(host.Resolver<ConsultarServicosInternalUseCase>());
    }

    [Fact]
    public void Os_validadores_da_assembly_devem_estar_registrados()
    {
        using var host = new CadastroHost();

        var validador = host.Resolver<FluentValidation.IValidator<CadastrarClienteRequest>>();

        Assert.False(validador.Validate(new CadastrarClienteRequest("", "", "sem-arroba", "1")).IsValid);
        Assert.True(validador.Validate(
            new CadastrarClienteRequest("12345678909", "Maria", "maria@example.invalid", "11999990000")).IsValid);
    }
}

public class ClientesControllerTests
{
    [Fact]
    public async Task Cadastrar_deve_devolver_created_com_o_identificador()
    {
        using var host = new CadastroHost();
        var controller = host.Clientes();

        var resultado = await controller.Cadastrar(
            new CadastrarClienteRequest("12345678909", "Maria", "maria@example.invalid", "11999990000"),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(nameof(ClientesController.ObterPorId), created.ActionName);
        Assert.NotNull(created.Value);
    }

    [Fact]
    public async Task Cadastrar_deve_recusar_documento_duplicado()
    {
        using var host = new CadastroHost();
        var controller = host.Clientes();
        var request = new CadastrarClienteRequest("12345678909", "Maria", "maria@example.invalid", "11999990000");
        await controller.Cadastrar(request, CancellationToken.None);

        var erro = await Assert.ThrowsAsync<OficinaException>(
            () => controller.Cadastrar(request, CancellationToken.None));

        Assert.Equal(409, erro.StatusHttp);
    }

    [Fact]
    public async Task Listar_e_obter_devem_refletir_o_cliente_cadastrado()
    {
        using var host = new CadastroHost();
        var controller = host.Clientes();
        var created = (CreatedAtActionResult)await controller.Cadastrar(
            new CadastrarClienteRequest("12345678909", "  Maria  ", "maria@example.invalid", "11999990000"),
            CancellationToken.None);
        var id = (Guid)created.RouteValues!["id"]!;

        var lista = Assert.IsType<OkObjectResult>(await controller.Listar(CancellationToken.None));
        var item = Assert.IsType<OkObjectResult>(await controller.ObterPorId(id, CancellationToken.None));

        Assert.NotNull(lista.Value);
        Assert.NotNull(item.Value);
    }

    [Fact]
    public async Task Atualizar_deve_devolver_no_content()
    {
        using var host = new CadastroHost();
        var controller = host.Clientes();
        var created = (CreatedAtActionResult)await controller.Cadastrar(
            new CadastrarClienteRequest("12345678909", "Maria", "maria@example.invalid", "11999990000"),
            CancellationToken.None);
        var id = (Guid)created.RouteValues!["id"]!;

        var resultado = await controller.Atualizar(
            id, new AtualizarClienteRequest("12345678909", "Maria Souza", "maria.souza@example.invalid", "11999990001"),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task Obter_deve_falhar_com_404_quando_o_cliente_nao_existe()
    {
        using var host = new CadastroHost();

        var erro = await Assert.ThrowsAsync<OficinaException>(
            () => host.Clientes().ObterPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, erro.StatusHttp);
    }
}

public class VeiculosControllerTests
{
    private static async Task<Guid> CadastrarCliente(CadastroHost host)
    {
        var created = (CreatedAtActionResult)await host.Clientes().Cadastrar(
            new CadastrarClienteRequest("12345678909", "Maria", "maria@example.invalid", "11999990000"),
            CancellationToken.None);
        return (Guid)created.RouteValues!["id"]!;
    }

    [Fact]
    public async Task Cadastrar_deve_criar_o_veiculo_do_cliente()
    {
        using var host = new CadastroHost();
        var clienteId = await CadastrarCliente(host);

        var resultado = await host.Veiculos().Cadastrar(
            new CadastrarVeiculoRequest(clienteId, "ABC1D23", "12345678901", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado);
    }

    [Fact]
    public async Task Cadastrar_deve_recusar_cliente_inexistente()
    {
        using var host = new CadastroHost();

        var erro = await Assert.ThrowsAsync<OficinaException>(() => host.Veiculos().Cadastrar(
            new CadastrarVeiculoRequest(Guid.NewGuid(), "ABC1D23", "12345678901", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None));

        Assert.Equal(404, erro.StatusHttp);
    }

    [Fact]
    public async Task Cadastrar_deve_recusar_placa_duplicada()
    {
        using var host = new CadastroHost();
        var clienteId = await CadastrarCliente(host);
        var controller = host.Veiculos();
        await controller.Cadastrar(
            new CadastrarVeiculoRequest(clienteId, "ABC1D23", "12345678901", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None);

        var erro = await Assert.ThrowsAsync<OficinaException>(() => controller.Cadastrar(
            new CadastrarVeiculoRequest(clienteId, "ABC1D23", "99999999999", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None));

        Assert.Equal(409, erro.StatusHttp);
    }

    [Fact]
    public async Task Cadastrar_deve_recusar_renavam_duplicado()
    {
        using var host = new CadastroHost();
        var clienteId = await CadastrarCliente(host);
        var controller = host.Veiculos();
        await controller.Cadastrar(
            new CadastrarVeiculoRequest(clienteId, "ABC1D23", "12345678901", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None);

        var erro = await Assert.ThrowsAsync<OficinaException>(() => controller.Cadastrar(
            new CadastrarVeiculoRequest(clienteId, "XYZ9W87", "12345678901", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None));

        Assert.Equal(409, erro.StatusHttp);
    }

    [Fact]
    public async Task Listar_obter_e_atualizar_devem_operar_sobre_o_veiculo_cadastrado()
    {
        using var host = new CadastroHost();
        var clienteId = await CadastrarCliente(host);
        var controller = host.Veiculos();
        var created = (CreatedAtActionResult)await controller.Cadastrar(
            new CadastrarVeiculoRequest(clienteId, "ABC1D23", "12345678901", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None);
        var id = (Guid)created.RouteValues!["id"]!;

        Assert.IsType<OkObjectResult>(await controller.Listar(CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.ObterPorId(id, CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Atualizar(
            id, new AtualizarVeiculoRequest("XYZ9W87", "99999999999", new ModeloRequest("Corolla", "Toyota", 2023)),
            CancellationToken.None));
    }
}

public class ServicosControllerTests
{
    [Fact]
    public async Task Deve_cadastrar_listar_obter_e_atualizar_um_servico()
    {
        using var host = new CadastroHost();
        var controller = host.Servicos();
        var pecaId = Guid.NewGuid();
        var insumoId = Guid.NewGuid();

        var created = (CreatedAtActionResult)await controller.Cadastrar(
            new CadastrarServicoRequest(150m, [new ItemRequeridoRequest(pecaId, 2)], [new ItemRequeridoRequest(insumoId, 1)]),
            CancellationToken.None);
        var id = (Guid)created.RouteValues!["id"]!;

        Assert.IsType<OkObjectResult>(await controller.Listar(CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.ObterPorId(id, CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Atualizar(
            id, new CadastrarServicoRequest(200m, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Obter_deve_falhar_com_404_para_servico_inexistente()
    {
        using var host = new CadastroHost();

        var erro = await Assert.ThrowsAsync<OficinaException>(
            () => host.Servicos().ObterPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, erro.StatusHttp);
    }
}

public class InternalControllerTests
{
    [Fact]
    public async Task Deve_expor_cliente_e_veiculo_por_identificador_e_por_chave_natural()
    {
        using var host = new CadastroHost();
        var createdCliente = (CreatedAtActionResult)await host.Clientes().Cadastrar(
            new CadastrarClienteRequest("12345678909", "Maria", "maria@example.invalid", "11999990000"),
            CancellationToken.None);
        var clienteId = (Guid)createdCliente.RouteValues!["id"]!;
        var createdVeiculo = (CreatedAtActionResult)await host.Veiculos().Cadastrar(
            new CadastrarVeiculoRequest(clienteId, "ABC1D23", "12345678901", new ModeloRequest("Civic", "Honda", 2022)),
            CancellationToken.None);
        var veiculoId = (Guid)createdVeiculo.RouteValues!["id"]!;

        var controller = host.Internal();

        Assert.IsType<OkObjectResult>(await controller.ObterCliente(clienteId, CancellationToken.None));
        // Pontuacao no documento e na placa e normalizada antes da consulta.
        Assert.IsType<OkObjectResult>(await controller.ObterClientePorDocumento("123.456.789-09", CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.ObterVeiculo(veiculoId, CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.ObterVeiculoPorPlaca("abc-1d23", CancellationToken.None));
    }

    [Fact]
    public async Task Consulta_de_servicos_deve_separar_encontrados_de_ausentes()
    {
        using var host = new CadastroHost();
        var created = (CreatedAtActionResult)await host.Servicos().Cadastrar(
            new CadastrarServicoRequest(100m, [new ItemRequeridoRequest(Guid.NewGuid(), 1)], null),
            CancellationToken.None);
        var existente = (Guid)created.RouteValues!["id"]!;
        var ausente = Guid.NewGuid();

        var ok = Assert.IsType<OkObjectResult>(await host.Internal().ConsultarServicos(
            new ConsultarServicosRequest([existente, ausente, existente]), CancellationToken.None));

        var resposta = Assert.IsType<ConsultarServicosResponse>(ok.Value);
        Assert.Single(resposta.Encontrados);
        Assert.Equal(existente, resposta.Encontrados[0].Id);
        Assert.Equal([ausente], resposta.Ausentes);
    }

    [Fact]
    public async Task Deve_falhar_com_404_para_cliente_e_veiculo_inexistentes()
    {
        using var host = new CadastroHost();
        var controller = host.Internal();

        Assert.Equal(404, (await Assert.ThrowsAsync<OficinaException>(
            () => controller.ObterCliente(Guid.NewGuid(), CancellationToken.None))).StatusHttp);
        Assert.Equal(404, (await Assert.ThrowsAsync<OficinaException>(
            () => controller.ObterClientePorDocumento("00000000000", CancellationToken.None))).StatusHttp);
        Assert.Equal(404, (await Assert.ThrowsAsync<OficinaException>(
            () => controller.ObterVeiculo(Guid.NewGuid(), CancellationToken.None))).StatusHttp);
        Assert.Equal(404, (await Assert.ThrowsAsync<OficinaException>(
            () => controller.ObterVeiculoPorPlaca("ZZZ0A00", CancellationToken.None))).StatusHttp);
    }
}

public class AdminFuncionariosControllerTests
{
    private static CriarFuncionarioRequest Request(string cpf = "12345678909")
        => new("Ana", cpf, "SenhaForte123!", "Admin");

    [Fact]
    public async Task Deve_criar_listar_e_obter_um_funcionario()
    {
        using var host = new CadastroHost();
        var controller = host.Funcionarios();

        var created = Assert.IsType<CreatedAtActionResult>(await controller.Criar(Request(), CancellationToken.None));
        var response = Assert.IsType<FuncionarioResponse>(created.Value);

        Assert.Equal("Admin", response.Perfil);
        Assert.True(response.Ativo);
        Assert.IsType<OkObjectResult>(await controller.Listar(CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.Obter(response.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Deve_recusar_cpf_duplicado()
    {
        using var host = new CadastroHost();
        var controller = host.Funcionarios();
        await controller.Criar(Request(), CancellationToken.None);

        var erro = await Assert.ThrowsAsync<OficinaException>(
            () => controller.Criar(Request(), CancellationToken.None));

        Assert.Equal(409, erro.StatusHttp);
    }

    [Fact]
    public async Task Deve_recusar_perfil_desconhecido()
    {
        using var host = new CadastroHost();

        var erro = await Assert.ThrowsAsync<OficinaException>(() => host.Funcionarios().Criar(
            new CriarFuncionarioRequest("Ana", "12345678909", "SenhaForte123!", "Sindico"),
            CancellationToken.None));

        Assert.Equal(400, erro.StatusHttp);
    }

    [Fact]
    public async Task Deve_atualizar_alterar_senha_e_alternar_status()
    {
        using var host = new CadastroHost();
        var controller = host.Funcionarios();
        var created = (CreatedAtActionResult)await controller.Criar(Request(), CancellationToken.None);
        var id = ((FuncionarioResponse)created.Value!).Id;

        var atualizado = Assert.IsType<OkObjectResult>(
            await controller.Atualizar(id, new AtualizarFuncionarioRequest("Ana Paula", "Funcionario", true), CancellationToken.None));
        Assert.Equal("Funcionario", ((FuncionarioResponse)atualizado.Value!).Perfil);

        Assert.IsType<NoContentResult>(await controller.AlterarSenha(
            id, new AlterarSenhaFuncionarioRequest("OutraSenha123!"), CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Inativar(id, CancellationToken.None));

        var inativo = Assert.IsType<OkObjectResult>(await controller.Obter(id, CancellationToken.None));
        Assert.False(((FuncionarioResponse)inativo.Value!).Ativo);

        Assert.IsType<NoContentResult>(await controller.Ativar(id, CancellationToken.None));
        var ativo = Assert.IsType<OkObjectResult>(await controller.Obter(id, CancellationToken.None));
        Assert.True(((FuncionarioResponse)ativo.Value!).Ativo);
    }

    [Fact]
    public async Task Deve_falhar_com_404_para_funcionario_inexistente()
    {
        using var host = new CadastroHost();

        var erro = await Assert.ThrowsAsync<OficinaException>(
            () => host.Funcionarios().Obter(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, erro.StatusHttp);
    }
}
