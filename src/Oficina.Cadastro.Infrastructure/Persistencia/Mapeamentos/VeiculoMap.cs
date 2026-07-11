using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Cadastro.Domain.Clientes;
using Oficina.Cadastro.Domain.Veiculos;

namespace Oficina.Cadastro.Infrastructure.Persistencia.Mapeamentos;

public class VeiculoMap : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> b)
    {
        b.ToTable("Veiculos");
        b.HasKey(x => x.Id);
        b.Property(x => x.ClienteId).IsRequired();

        b.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        b.OwnsOne(x => x.Placa, placa =>
        {
            placa.Property(x => x.Valor)
                .HasColumnName("Placa")
                .HasMaxLength(7)
                .IsRequired();
            placa.HasIndex(x => x.Valor).IsUnique();
        });

        b.OwnsOne(x => x.Renavam, renavam =>
        {
            renavam.Property(x => x.Valor)
                .HasColumnName("Renavam")
                .HasMaxLength(11)
                .IsRequired();
            renavam.HasIndex(x => x.Valor).IsUnique();
        });

        b.OwnsOne(x => x.Modelo, modelo =>
        {
            modelo.Property(x => x.Descricao).HasColumnName("ModeloDescricao").HasMaxLength(100).IsRequired();
            modelo.Property(x => x.Marca).HasColumnName("ModeloMarca").HasMaxLength(100).IsRequired();
            modelo.Property(x => x.Ano).HasColumnName("ModeloAno").IsRequired();
        });
    }
}
