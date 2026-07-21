using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Cadastro.Application.UseCases.CatalogoEstoque;
using Oficina.Cadastro.Application.UseCases.Clientes;
using Oficina.Cadastro.Application.UseCases.Internal;
using Oficina.Cadastro.Application.UseCases.Seguranca;
using Oficina.Cadastro.Application.UseCases.Veiculos;

namespace Oficina.Cadastro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCadastroApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<CadastrarClienteUseCase>();
        services.AddScoped<AtualizarClienteUseCase>();
        services.AddScoped<ListarClientesUseCase>();
        services.AddScoped<ObterClienteUseCase>();
        services.AddScoped<CadastrarVeiculoUseCase>();
        services.AddScoped<AtualizarVeiculoUseCase>();
        services.AddScoped<ListarVeiculosUseCase>();
        services.AddScoped<ObterVeiculoUseCase>();
        services.AddScoped<ListarVeiculosPorClienteUseCase>();

        services.AddScoped<CadastrarServicoUseCase>();
        services.AddScoped<ListarServicosUseCase>();
        services.AddScoped<ObterServicoUseCase>();
        services.AddScoped<AtualizarServicoUseCase>();

        services.AddScoped<CriarFuncionarioUseCase>();
        services.AddScoped<ListarFuncionariosUseCase>();
        services.AddScoped<ObterFuncionarioUseCase>();
        services.AddScoped<AtualizarFuncionarioUseCase>();
        services.AddScoped<AlterarSenhaFuncionarioUseCase>();
        services.AddScoped<AlterarStatusFuncionarioUseCase>();

        services.AddScoped<ObterClienteInternalUseCase>();
        services.AddScoped<ObterClientePorDocumentoInternalUseCase>();
        services.AddScoped<ObterVeiculoInternalUseCase>();
        services.AddScoped<ObterVeiculoPorPlacaInternalUseCase>();
        services.AddScoped<ConsultarServicosInternalUseCase>();

        return services;
    }
}
