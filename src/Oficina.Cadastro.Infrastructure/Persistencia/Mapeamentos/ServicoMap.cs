using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Cadastro.Domain.CatalogoEstoque;

namespace Oficina.Cadastro.Infrastructure.Persistencia.Mapeamentos;

public class ServicoMap : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> b)
    {
        b.ToTable("Servicos");
        b.HasKey(x => x.Id);
        b.Property(x => x.MaoDeObra).HasColumnType("decimal(18,2)").IsRequired();

        b.OwnsMany(x => (ICollection<ServicoPecaRequerida>)x.Pecas, items =>
        {
            items.ToTable("ServicoPecasRequeridas");
            items.WithOwner().HasForeignKey("ServicoId");
            items.HasKey(x => x.Id);
            items.Property(x => x.Id).ValueGeneratedNever();
            items.Property(x => x.PecaId).IsRequired();
            items.Property(x => x.Quantidade).IsRequired();
        });

        b.OwnsMany(x => (ICollection<ServicoInsumoRequerido>)x.Insumos, items =>
        {
            items.ToTable("ServicoInsumosRequeridos");
            items.WithOwner().HasForeignKey("ServicoId");
            items.HasKey(x => x.Id);
            items.Property(x => x.Id).ValueGeneratedNever();
            items.Property(x => x.InsumoId).IsRequired();
            items.Property(x => x.Quantidade).IsRequired();
        });
    }
}
