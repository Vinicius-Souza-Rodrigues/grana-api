using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Data.Configurations;

public sealed class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        builder.ToTable("transferencias");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Valor).IsRequired();

        builder.Property(t => t.Data).IsRequired();

        builder.Property(t => t.Descricao).HasMaxLength(Transferencia.TamanhoMaximoDescricao);

        builder.Property(t => t.CriadoEm).IsRequired();

        builder.HasOne(t => t.ContaOrigem)
            .WithMany()
            .HasForeignKey(t => t.IdContaOrigem)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ContaDestino)
            .WithMany()
            .HasForeignKey(t => t.IdContaDestino)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
