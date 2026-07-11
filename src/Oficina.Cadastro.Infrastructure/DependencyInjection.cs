using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Infrastructure.Persistencia;
using Oficina.Cadastro.Infrastructure.Repositorios;

namespace Oficina.Cadastro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCadastroInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OficinaCadastroDb")
            ?? configuration.GetConnectionString("SqlServer")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=OficinaCadastroDb;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<CadastroDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<ICadastroRepository, CadastroRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();

        return services;
    }
}
