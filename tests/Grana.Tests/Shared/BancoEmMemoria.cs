using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tests.Shared;

public static class BancoEmMemoria
{
    public static GranaDbContext CriarContexto()
        => CriarContexto(Guid.NewGuid().ToString());

    public static GranaDbContext CriarContexto(string nomeBanco)
    {
        var opcoes = new DbContextOptionsBuilder<GranaDbContext>()
            .UseInMemoryDatabase(nomeBanco)
            .Options;

        return new GranaDbContext(opcoes);
    }
}
