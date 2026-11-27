using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Services;
using Grana.Api.Autenticacao;
using Grana.Api.Middlewares;
using Grana.Api.OpenApi;
using Infraestructure.Data;
using Infraestructure.Repositories;
using Infraestructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddDbContext<GranaDbContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Grana"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IContaService, ContaService>();
builder.Services.AddScoped<IContaRepository, ContaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ILancamentoService, LancamentoService>();
builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<ITransferenciaService, TransferenciaService>();
builder.Services.AddScoped<ITransferenciaRepository, TransferenciaRepository>();
builder.Services.AddScoped<IOrcamentoService, OrcamentoService>();
builder.Services.AddScoped<IResumoService, ResumoService>();
builder.Services.AddScoped<IExtratoService, ExtratoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddSingleton<IHashSenha, HashSenha>();
builder.Services.AddSingleton<IGeradorToken, GeradorToken>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarAutenticacaoJwt(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GranaDbContext>();
builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer<DocumentoOpenApiTransformer>();
    o.AddOperationTransformer<ExemplosOpenApiTransformer>();
    o.AddOperationTransformer<SegurancaOpenApiTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("SeedDemo"))
{
    using var escopo = app.Services.CreateScope();
    var seedDemo = new SeedDemo(escopo.ServiceProvider.GetRequiredService<GranaDbContext>());
    await seedDemo.Executar(DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference().AllowAnonymous();

app.MapControllers();

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
