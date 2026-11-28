using System.Security.Cryptography;
using Infraestructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Shared;

public sealed class GranaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private NpgsqlConnection _conexao;
    private Respawner _respawner;

    public string ChaveJwt { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using (var escopo = Services.CreateScope())
            await escopo.ServiceProvider.GetRequiredService<GranaDbContext>().Database.MigrateAsync();

        _conexao = new NpgsqlConnection(_postgres.GetConnectionString());
        await _conexao.OpenAsync();

        _respawner = await Respawner.CreateAsync(_conexao, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public Task LimparBanco() => _respawner.ResetAsync(_conexao);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _conexao.DisposeAsync();
        await _postgres.DisposeAsync();
        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Grana", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Chave", ChaveJwt);
    }
}
