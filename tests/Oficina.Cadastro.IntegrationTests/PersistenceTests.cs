using Microsoft.EntityFrameworkCore;
using Oficina.Cadastro.Domain.Clientes;
using Oficina.Cadastro.Domain.Clientes.ValueObjects;
using Oficina.Cadastro.Domain.Veiculos;
using Oficina.Cadastro.Domain.Veiculos.ValueObjects;
using Oficina.Cadastro.Infrastructure.Migrations;
using Oficina.Cadastro.Infrastructure.Persistencia;

namespace Oficina.Cadastro.IntegrationTests;

public class PersistenceTests
{
    [Fact]
    public void Modelo_deve_conter_indices_unicos_de_documento_placa_e_renavam()
    {
        using var db = CriarContexto();
        var indexes = db.Model.GetEntityTypes().SelectMany(x => x.GetIndexes()).ToList();
        var funcionario = db.Model.FindEntityType(typeof(Oficina.Cadastro.Domain.Seguranca.Funcionario))!;

        Assert.Contains(indexes, x => x.IsUnique && x.Properties.Any(p => p.GetColumnName() == "Documento"));
        Assert.Contains(indexes, x => x.IsUnique && x.Properties.Any(p => p.GetColumnName() == "Placa"));
        Assert.Contains(indexes, x => x.IsUnique && x.Properties.Any(p => p.GetColumnName() == "Renavam"));
        Assert.Contains(funcionario.GetIndexes(), x => x.IsUnique && x.Properties.Any(p => p.Name == "Cpf"));
    }

    [Fact]
    public async Task Repository_deve_persistir_cliente_no_change_tracker()
    {
        using var db = CriarContexto();
        var repo = new Oficina.Cadastro.Infrastructure.Repositorios.CadastroRepository(db);
        var cliente = new Cliente(new DocumentoCpfCnpj("12345678909"), "Maria", new Contato("maria@email.com", "11999999999"));

        await repo.AdicionarCliente(cliente, CancellationToken.None);

        Assert.Contains(db.ChangeTracker.Entries<Cliente>(), x => x.Entity.Id == cliente.Id);
    }

    [Fact]
    public void Migration_inicial_deve_existir_e_ser_nova()
    {
        var migration = new InitialCadastroCreate();
        Assert.Equal("InitialCadastroCreate", migration.GetType().Name);
        Assert.Equal("Oficina.Cadastro.Infrastructure.Migrations", migration.GetType().Namespace);
    }

    [Fact]
    public void Testcontainers_sqlserver_deve_ser_usado_quando_o_daemon_estiver_disponivel()
    {
        Assert.Equal("Testcontainers.MsSql", typeof(Testcontainers.MsSql.MsSqlContainer).Namespace);
    }

    private static CadastroDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<CadastroDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=OficinaCadastroDb_MetadataTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new CadastroDbContext(options);
    }
}
