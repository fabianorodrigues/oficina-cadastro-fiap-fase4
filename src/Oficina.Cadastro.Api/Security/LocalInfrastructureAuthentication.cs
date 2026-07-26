using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Oficina.Cadastro.Application.Abstractions.Seguranca;
using Oficina.Cadastro.Domain.Seguranca;
using Oficina.Cadastro.Domain.Seguranca.Enums;
using Oficina.Cadastro.Infrastructure.Persistencia;

namespace Oficina.Cadastro.Api.Security;

public static class LocalInfrastructureAuthentication
{
    public static async Task SeedLocalAdminIfEnabled(this WebApplication app)
    {
        if (!IsLocalSeedEnabled(app))
        {
            return;
        }

        var cpf = Required(app.Configuration, "LocalInfrastructure:SeedAdmin:Cpf");
        var password = Required(app.Configuration, "LocalInfrastructure:SeedAdmin:Password");
        var name = app.Configuration["LocalInfrastructure:SeedAdmin:Name"] ?? "Administrador Local";

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CadastroDbContext>();
        var passwordHash = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();

        var normalizedCpf = Funcionario.NormalizarCpf(cpf);
        var existing = await db.Funcionarios.FirstOrDefaultAsync(x => x.Cpf == normalizedCpf);
        if (existing is not null)
        {
            var changed = false;
            if (existing.Perfil != PerfilUsuarioInterno.Admin || !existing.Ativo || !string.Equals(existing.Nome, name, StringComparison.Ordinal))
            {
                existing.Atualizar(name, PerfilUsuarioInterno.Admin, ativo: true);
                changed = true;
            }

            if (app.Configuration.GetValue("LocalInfrastructure:SeedAdmin:ResetPassword", true) &&
                !passwordHash.Verificar(existing.SenhaHash, password))
            {
                existing.AlterarSenhaHash(passwordHash.Hash(password));
                changed = true;
            }

            if (changed)
            {
                await db.SaveChangesAsync();
            }

            return;
        }

        db.Funcionarios.Add(new Funcionario(name, normalizedCpf, passwordHash.Hash(password), PerfilUsuarioInterno.Admin));
        await db.SaveChangesAsync();
    }

    public static IEndpointRouteBuilder MapLocalAuthenticationEndpoints(this WebApplication app)
    {
        if (!IsLocalAuthEnabled(app))
        {
            return app;
        }

        app.MapPost("/api/auth/cpf", async (
                LocalLoginCpfRequest request,
                CadastroDbContext db,
                IPasswordHashService passwordHash,
                IConfiguration configuration,
                CancellationToken ct) =>
            {
                var password = request.Password ?? request.Senha;
                var normalizedCpf = Funcionario.NormalizarCpf(request.Cpf);
                var funcionario = await db.Funcionarios.FirstOrDefaultAsync(x => x.Cpf == normalizedCpf, ct);
                if (funcionario is null ||
                    !funcionario.Ativo ||
                    string.IsNullOrWhiteSpace(password) ||
                    !passwordHash.Verificar(funcionario.SenhaHash, password))
                {
                    return Results.Unauthorized();
                }

                var role = funcionario.Perfil.ToString();
                var token = CreateToken(
                    funcionario.Id.ToString("D"),
                    funcionario.Cpf,
                    role,
                    funcionario.Nome,
                    Required(configuration, "LocalInfrastructure:Auth:SigningKey"),
                    configuration.GetValue("LocalInfrastructure:Auth:ExpirationMinutes", 60));

                return Results.Ok(new
                {
                    accessToken = token,
                    tokenType = "Bearer",
                    expiresIn = configuration.GetValue("LocalInfrastructure:Auth:ExpirationMinutes", 60) * 60,
                    user = new
                    {
                        name = funcionario.Nome,
                        role
                    }
                });
            })
            .AllowAnonymous();

        return app;
    }

    private static bool IsLocalAuthEnabled(WebApplication app)
        => app.Environment.IsDevelopment() &&
           app.Configuration.GetValue("LocalInfrastructure:Enabled", false) &&
           app.Configuration.GetValue("LocalInfrastructure:Auth:Enabled", false);

    private static bool IsLocalSeedEnabled(WebApplication app)
        => app.Environment.IsDevelopment() &&
           app.Configuration.GetValue("LocalInfrastructure:Enabled", false) &&
           app.Configuration.GetValue("LocalInfrastructure:SeedAdmin:Enabled", false);

    private static string CreateToken(
        string subject,
        string cpf,
        string role,
        string name,
        string signingKey,
        int expirationMinutes)
    {
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("LocalInfrastructure__Auth__SigningKey deve possuir ao menos 32 bytes.");
        }

        var now = DateTimeOffset.UtcNow;
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "HS256",
            typ = "JWT"
        }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = subject,
            cpf,
            role,
            name,
            iss = "oficina-local",
            aud = "oficina-local-gateway",
            iat = now.ToUnixTimeSeconds(),
            exp = now.AddMinutes(expirationMinutes).ToUnixTimeSeconds(),
            jti = Guid.NewGuid().ToString("N")
        }));

        var unsignedToken = $"{header}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var signature = Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
        return $"{unsignedToken}.{signature}";
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} obrigatorio para infraestrutura local.");
        }

        return value;
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record LocalLoginCpfRequest(string Cpf, string? Password, string? Senha);
}
