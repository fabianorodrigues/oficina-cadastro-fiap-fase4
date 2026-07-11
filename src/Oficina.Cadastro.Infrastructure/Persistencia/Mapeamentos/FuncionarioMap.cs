using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Cadastro.Domain.Seguranca;

namespace Oficina.Cadastro.Infrastructure.Persistencia.Mapeamentos;

public class FuncionarioMap : IEntityTypeConfiguration<Funcionario>
{
    public void Configure(EntityTypeBuilder<Funcionario> b)
    {
        b.ToTable("Funcionarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Cpf).HasMaxLength(11).IsRequired();
        b.HasIndex(x => x.Cpf).IsUnique();
        b.Property(x => x.SenhaHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.Perfil).IsRequired();
        b.Property(x => x.Ativo).IsRequired();
        b.Property(x => x.DataCriacao).IsRequired();
    }
}
