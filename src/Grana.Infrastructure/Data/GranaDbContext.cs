using Domain.Models;
using Infraestructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data;

public sealed class GranaDbContext(DbContextOptions<GranaDbContext> options) : DbContext(options)
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AtualizarVersaoDasContas();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AtualizarVersaoDasContas();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GranaDbContext).Assembly);
    }

    private void AtualizarVersaoDasContas()
    {
        foreach (var entrada in ChangeTracker.Entries<Conta>())
        {
            if (entrada.State == EntityState.Added || entrada.State == EntityState.Modified)
                entrada.Property(ContaConfiguration.Versao).CurrentValue = Guid.NewGuid();
        }
    }
}
