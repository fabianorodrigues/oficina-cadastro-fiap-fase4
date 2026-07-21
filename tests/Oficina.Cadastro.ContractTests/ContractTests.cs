using Microsoft.AspNetCore.Mvc;
using Oficina.Cadastro.Api.Controllers;
using Oficina.Cadastro.Application.DTO.CatalogoEstoque;
using Oficina.Cadastro.Application.DTO.Internal;

namespace Oficina.Cadastro.ContractTests;

public class ContractTests
{
    [Fact]
    public void Rotas_publicas_devem_preservar_templates_definidos()
    {
        AssertRoute<ClientesController>("api/clientes");
        AssertRoute<VeiculosController>("api/veiculos");
        AssertRoute<AdminFuncionariosController>("api/admin/funcionarios");
        AssertRoute<ServicosController>("api/servicos");
    }

    [Fact]
    public void Consulta_interna_de_servicos_deve_aceitar_ids_em_lote()
    {
        var id = Guid.NewGuid();
        var request = new ConsultarServicosRequest([id]);
        Assert.Equal(id, Assert.Single(request.Ids));
    }

    [Fact]
    public void Response_interno_de_servico_deve_expor_mao_de_obra_e_referencias_opacas()
    {
        var response = new ServicoInternalResponse
        {
            Id = Guid.NewGuid(),
            MaoDeObra = 10m,
            Pecas = [new ReferenciaMaterialInternalResponse { ReferenciaId = Guid.NewGuid(), Quantidade = 2 }],
            Insumos = [new ReferenciaMaterialInternalResponse { ReferenciaId = Guid.NewGuid(), Quantidade = 3 }]
        };

        Assert.Equal(10m, response.MaoDeObra);
        Assert.Single(response.Pecas);
        Assert.Single(response.Insumos);
    }

    [Fact]
    public void Servico_publico_deve_manter_shape_de_request_estavel()
    {
        var request = new CadastrarServicoRequest(100m, [new ItemRequeridoRequest(Guid.NewGuid(), 1)], null);
        Assert.Equal(100m, request.MaoDeObra);
        Assert.Single(request.Pecas!);
    }

    private static void AssertRoute<T>(string expected)
    {
        var route = typeof(T).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single();
        Assert.Equal(expected, route.Template);
    }
}
