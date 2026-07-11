using Microsoft.EntityFrameworkCore;
using Oficina.Cadastro.Domain.CatalogoEstoque;
using Oficina.Cadastro.Domain.Clientes;
using Oficina.Cadastro.Domain.Seguranca;
using Oficina.Cadastro.Domain.Veiculos;
using Oficina.Cadastro.Infrastructure.Persistencia.Mapeamentos;

namespace Oficina.Cadastro.Infrastructure.Persistencia;

public class CadastroDbContext(DbContextOptions<CadastroDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Servico> Servicos => Set<Servico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClienteMap());
        modelBuilder.ApplyConfiguration(new VeiculoMap());
        modelBuilder.ApplyConfiguration(new FuncionarioMap());
        modelBuilder.ApplyConfiguration(new ServicoMap());
    }
}
