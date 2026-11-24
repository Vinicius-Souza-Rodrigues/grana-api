using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Services;
using Grana.Api.Middlewares;
using Infraestructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddScoped<IContaService, ContaService>();
builder.Services.AddSingleton<IContaRepository, ContaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddSingleton<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ILancamentoService, LancamentoService>();
builder.Services.AddSingleton<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<ITransferenciaService, TransferenciaService>();
builder.Services.AddSingleton<ITransferenciaRepository, TransferenciaRepository>();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
