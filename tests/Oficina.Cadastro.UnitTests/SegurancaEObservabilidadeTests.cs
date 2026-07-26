using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oficina.Cadastro.Api.Middleware;
using Oficina.Cadastro.Api.Observability;
using Oficina.Cadastro.Api.Security;

namespace Oficina.Cadastro.UnitTests;

public class PasswordHashServiceTests
{
    private readonly PasswordHashService _service = new();

    [Fact]
    public void Hash_deve_produzir_formato_pbkdf2_com_cem_mil_iteracoes()
    {
        var hash = _service.Hash("SenhaForte123!");

        var partes = hash.Split('$');
        Assert.Equal(4, partes.Length);
        Assert.Equal("PBKDF2-SHA256", partes[0]);
        // O Lambda de autenticacao recusa hashes com menos de 100000 iteracoes.
        Assert.Equal(100000, int.Parse(partes[1]));
        Assert.Equal(16, Convert.FromBase64String(partes[2]).Length);
        Assert.Equal(32, Convert.FromBase64String(partes[3]).Length);
    }

    [Fact]
    public void Hash_deve_gerar_salt_diferente_para_a_mesma_senha()
    {
        Assert.NotEqual(_service.Hash("SenhaForte123!"), _service.Hash("SenhaForte123!"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hash_deve_recusar_senha_vazia(string senha)
    {
        Assert.Throws<ArgumentException>(() => _service.Hash(senha));
    }

    [Fact]
    public void Verificar_deve_aceitar_a_senha_correta()
    {
        var hash = _service.Hash("SenhaForte123!");
        Assert.True(_service.Verificar(hash, "SenhaForte123!"));
    }

    [Fact]
    public void Verificar_deve_recusar_senha_incorreta()
    {
        var hash = _service.Hash("SenhaForte123!");
        Assert.False(_service.Verificar(hash, "SenhaErrada123!"));
    }

    [Theory]
    [InlineData("", "senha")]
    [InlineData("hash", "")]
    [InlineData("formato-invalido", "senha")]
    [InlineData("PBKDF2-SHA256$abc$c2FsdA==$aGFzaA==", "senha")]
    [InlineData("OUTRO-ALGORITMO$100000$c2FsdA==$aGFzaA==", "senha")]
    public void Verificar_deve_recusar_entrada_malformada_sem_lancar(string senhaHash, string senha)
    {
        Assert.False(_service.Verificar(senhaHash, senha));
    }
}

public class DevelopmentAuthenticationHandlerTests
{
    private static async Task<AuthenticateResult> Autenticar(Action<HttpContext> configurar)
    {
        var handler = new DevelopmentAuthenticationHandler(
            new OptionsMonitorStub(),
            NullLoggerFactory.Instance,
            UrlEncoder.Default);

        var scheme = new AuthenticationScheme(
            DevelopmentAuthenticationDefaults.Scheme,
            DevelopmentAuthenticationDefaults.Scheme,
            typeof(DevelopmentAuthenticationHandler));

        var context = new DefaultHttpContext();
        configurar(context);

        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task Deve_ficar_sem_resultado_quando_o_cabecalho_de_papel_esta_ausente()
    {
        var resultado = await Autenticar(_ => { });

        Assert.False(resultado.Succeeded);
        Assert.Null(resultado.Failure);
        Assert.Null(resultado.Ticket);
    }

    [Fact]
    public async Task Deve_falhar_quando_o_papel_e_desconhecido()
    {
        var resultado = await Autenticar(ctx => ctx.Request.Headers["X-Dev-Role"] = "Sindico");

        Assert.False(resultado.Succeeded);
        Assert.Equal("Invalid X-Dev-Role.", resultado.Failure?.Message);
    }

    [Theory]
    [InlineData("Funcionario")]
    [InlineData("funcionario")]
    [InlineData("ADMIN")]
    [InlineData("Cliente")]
    public async Task Deve_aceitar_papel_valido_sem_diferenciar_maiusculas(string papel)
    {
        var resultado = await Autenticar(ctx => ctx.Request.Headers["X-Dev-Role"] = papel);

        Assert.True(resultado.Succeeded);
        Assert.Single(resultado.Principal!.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task Deve_normalizar_o_papel_para_a_forma_canonica()
    {
        var resultado = await Autenticar(ctx => ctx.Request.Headers["X-Dev-Role"] = "admin");

        Assert.Equal("Admin", resultado.Principal!.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public async Task Deve_projetar_cpf_e_identificadores_quando_informados()
    {
        var clienteId = Guid.NewGuid();
        var funcionarioId = Guid.NewGuid();

        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers["X-Dev-Role"] = "Funcionario";
            ctx.Request.Headers["X-Dev-Cpf"] = "12345678901";
            ctx.Request.Headers["X-Dev-ClienteId"] = clienteId.ToString();
            ctx.Request.Headers["X-Dev-FuncionarioId"] = funcionarioId.ToString();
        });

        Assert.True(resultado.Succeeded);
        var principal = resultado.Principal!;
        Assert.Equal("12345678901", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("12345678901", principal.FindFirstValue("cpf"));
        Assert.Equal(clienteId.ToString("D"), principal.FindFirstValue("clienteId"));
        Assert.Equal(funcionarioId.ToString("D"), principal.FindFirstValue("funcionarioId"));
    }

    [Fact]
    public async Task Deve_usar_identificador_padrao_quando_o_cpf_nao_e_informado()
    {
        var resultado = await Autenticar(ctx => ctx.Request.Headers["X-Dev-Role"] = "Cliente");

        Assert.Equal("development-user", resultado.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Null(resultado.Principal.FindFirstValue("cpf"));
    }

    [Theory]
    [InlineData("X-Dev-ClienteId")]
    [InlineData("X-Dev-FuncionarioId")]
    public async Task Deve_falhar_quando_o_identificador_nao_e_um_guid(string cabecalho)
    {
        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers["X-Dev-Role"] = "Funcionario";
            ctx.Request.Headers[cabecalho] = "nao-e-guid";
        });

        Assert.False(resultado.Succeeded);
        Assert.Equal($"Invalid {cabecalho}.", resultado.Failure?.Message);
    }

    private sealed class OptionsMonitorStub : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}

public class OpenTelemetryRegistrationTests
{
    [Fact]
    public void Deve_ignorar_o_registro_quando_endpoint_nao_esta_configurado()
    {
        var services = new ServiceCollection();

        services.AddOpenTelemetryFailOpen(
            Configuracao(),
            new LoggingBuilderStub(services),
            "oficina-cadastro");

        Assert.DoesNotContain(services, x => x.ServiceType.FullName?.Contains("OpenTelemetry") == true);
    }

    [Fact]
    public void Deve_registrar_exporter_quando_o_endpoint_esta_configurado()
    {
        var services = new ServiceCollection();

        services.AddOpenTelemetryFailOpen(
            Configuracao(
                ("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector.example.invalid:4317")),
            new LoggingBuilderStub(services),
            "oficina-cadastro");

        Assert.Contains(services, x => x.ServiceType.FullName?.Contains("OpenTelemetry") == true);
    }

    [Fact]
    public void Deve_seguir_em_frente_quando_a_configuracao_de_telemetria_explode()
    {
        var services = new ServiceCollection();

        // Fail-open: telemetria e diagnostico, nao funcionalidade. Uma falha ao
        // configurar o exporter nao pode impedir a aplicacao de subir.
        var excecao = Record.Exception(() => services.AddOpenTelemetryFailOpen(
            new ConfiguracaoQueFalha(),
            new LoggingBuilderStub(services),
            "oficina-cadastro"));

        Assert.Null(excecao);
        Assert.Contains(services, x => x.ImplementationFactory is not null);
    }

    private static IConfiguration Configuracao(params (string Chave, string Valor)[] valores)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(valores.Select(x => new KeyValuePair<string, string?>(x.Chave, x.Valor)))
            .Build();

    private sealed class ConfiguracaoQueFalha : IConfiguration
    {
        public string? this[string key]
        {
            get => throw new InvalidOperationException("Provedor de configuracao indisponivel.");
            set => throw new InvalidOperationException("Provedor de configuracao indisponivel.");
        }

        public IEnumerable<IConfigurationSection> GetChildren() => throw new InvalidOperationException();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new InvalidOperationException();
        public IConfigurationSection GetSection(string key) => throw new InvalidOperationException();
    }

    private sealed class LoggingBuilderStub(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Deve_preservar_o_correlation_id_recebido()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "correlacao-externa";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.Equal("correlacao-externa", context.Items[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("correlacao-externa", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task Deve_gerar_correlation_id_quando_ausente()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HeaderName]);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task Deve_gerar_correlation_id_quando_o_cabecalho_vem_em_branco()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "   ";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.True(Guid.TryParse((string)context.Items[CorrelationIdMiddleware.HeaderName]!, out _));
    }

    [Fact]
    public async Task Deve_chamar_o_proximo_middleware_da_cadeia()
    {
        var chamado = false;
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(
            _ => { chamado = true; return Task.CompletedTask; },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.True(chamado);
    }
}
