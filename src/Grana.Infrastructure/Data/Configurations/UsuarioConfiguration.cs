using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Data.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(Usuario.TamanhoMaximoNome);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(Usuario.TamanhoMaximoEmail);

        builder.Property(u => u.SenhaHash).IsRequired();

        builder.Property(u => u.CriadoEm).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
