namespace Oficina.Cadastro.Api.Configuration;

public static class CadastroConfigurationExtensions
{
    public static void ValidateCadastroProductionConfiguration(
        this IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("OficinaCadastroDb")))
        {
            throw new InvalidOperationException("Connection string obrigatoria nao foi configurada.");
        }

        if (string.Equals(configuration["Authentication:Mode"], "Development", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Modo de autenticacao Development nao pode ser utilizado em Production.");
        }
    }
}
