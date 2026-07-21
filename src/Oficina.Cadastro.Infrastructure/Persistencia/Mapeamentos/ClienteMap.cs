using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Cadastro.Domain.Clientes;

namespace Oficina.Cadastro.Infrastructure.Persistencia.Mapeamentos;

public class ClienteMap : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("Clientes");
        b.HasKey(x => x.Id);

        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();

        b.OwnsOne(x => x.Documento, doc =>
        {
            doc.Property(x => x.Valor)
                .HasColumnName("Documento")
                .HasMaxLength(14)
                .IsRequired();
            doc.HasIndex(x => x.Valor).IsUnique();
        });

        b.OwnsOne(x => x.Contato, contato =>
        {
            contato.Property(x => x.Email)
                .HasColumnName("ContatoEmail")
                .HasMaxLength(150)
                .IsRequired();
            contato.Property(x => x.Telefone)
                .HasColumnName("ContatoTelefone")
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
