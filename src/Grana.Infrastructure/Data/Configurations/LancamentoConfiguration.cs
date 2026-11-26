using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Data.Configurations;

public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.TipoLancamento).IsRequired();

        builder.Property(l => l.Valor).IsRequired();

        builder.Property(l => l.Data).IsRequired();

        builder.Property(l => l.Descricao).HasMaxLength(Lancamento.TamanhoMaximoDescricao);

        builder.Property(l => l.CriadoEm).IsRequired();

        builder.HasIndex(l => l.Data);

        builder.HasOne(l => l.Conta)
            .WithMany()
            .HasForeignKey(l => l.IdConta)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Categoria)
            .WithMany()
            .HasForeignKey(l => l.IdCategoria)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
