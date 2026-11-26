using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data;

public sealed class GranaDbContext(DbContextOptions<GranaDbContext> options) : DbContext(options)
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GranaDbContext).Assembly);
    }
}
