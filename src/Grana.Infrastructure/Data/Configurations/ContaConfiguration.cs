using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Data.Configurations;

public sealed class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("contas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(Conta.TamanhoMaximoNome);

        builder.Property(c => c.TipoConta).IsRequired();

        builder.Property(c => c.Saldo).IsRequired();

        builder.Property(c => c.SaldoInicial).IsRequired();

        builder.Property(c => c.StatusAtividadeConta).IsRequired();

        builder.Property(c => c.CriadoEm).IsRequired();
    }
}
