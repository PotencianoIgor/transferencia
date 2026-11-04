using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Transferencias.Domain;
using Transferencias.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Banco")));
builder.Services.AddScoped<IContaRepository, ContaRepository>();
builder.Services.AddScoped<TransferenciaService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception e) when (StatusCodeDe(e) is int statusCode)
    {
        await Results.Problem(e.Message, statusCode: statusCode).ExecuteAsync(context);
    }
});

app.MapPost("/transferencias", async (TransferenciaRequest request, TransferenciaService service) =>
{
    var transferencia = await service.TransferirAsync(request.Origem, request.Destino, request.ValorCentavos);
    return Results.Json(transferencia, statusCode: StatusCodes.Status201Created);
});

app.MapGet("/contas/{id}", (string id, TransferenciaService service) =>
    service.ObterContaAsync(id));

app.MapGet("/contas/{id}/movimentacoes", (string id, TransferenciaService service) =>
    service.ListarMovimentacoesAsync(id));

app.Run();

static int? StatusCodeDe(Exception e) => e switch
{
    EntradaInvalidaException => StatusCodes.Status400BadRequest,
    ContaNaoEncontradaException => StatusCodes.Status404NotFound,
    SaldoInsuficienteException => StatusCodes.Status409Conflict,
    _ => null
};

record TransferenciaRequest(string Origem, string Destino, long ValorCentavos);
