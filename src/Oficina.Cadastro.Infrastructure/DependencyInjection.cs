using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Infrastructure.Persistencia;
using Oficina.Cadastro.Infrastructure.Repositorios;

namespace Oficina.Cadastro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCadastroInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool allowLocalFallback)
    {
        var connectionString = configuration.GetConnectionString("OficinaCadastroDb")
            ?? configuration.GetConnectionString("SqlServer");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (!allowLocalFallback)
            {
                throw new InvalidOperationException("Connection string obrigatoria nao foi configurada.");
            }

            connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=OficinaCadastroDb;Trusted_Connection=True;TrustServerCertificate=True";
        }

        services.AddDbContext<CadastroDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<ICadastroRepository, CadastroRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();

        return services;
    }
}
