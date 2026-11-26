using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Data.Configurations;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(Categoria.TamanhoMaximoNome);

        builder.Property(c => c.TipoCategoria).IsRequired();

        builder.Property(c => c.OrcamentoMensal);

        builder.HasIndex(c => new { c.TipoCategoria, c.Nome }).IsUnique();
    }
}
