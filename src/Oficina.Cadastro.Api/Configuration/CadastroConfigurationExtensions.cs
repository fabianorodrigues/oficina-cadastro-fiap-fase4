namespace Oficina.Cadastro.Api.Configuration;

public static class CadastroConfigurationExtensions
{
    public const string SecretsDirectory = "/mnt/secrets-store";

    public static IConfigurationBuilder AddCadastroKeyPerFile(
        this IConfigurationBuilder configuration,
        IWebHostEnvironment environment)
    {
        if (Directory.Exists(SecretsDirectory) || environment.IsProduction())
        {
            configuration.AddKeyPerFile(
                directoryPath: SecretsDirectory,
                optional: true,
                reloadOnChange: false);
        }

        return configuration;
    }

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

        // ECS injects Secrets Manager values as environment variables, while the
        // previous runtime mounted them as files. A valid connection string is
        // the required production contract.
    }
}
