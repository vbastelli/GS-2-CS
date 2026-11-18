using System.Reflection;
using System.Text.Json.Serialization;
using AguaNaRua.Api.Api;
using AguaNaRua.Api.Application;
using AguaNaRua.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var seedOnly = args.Contains("--seed", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--seed").ToArray());

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<AtualizacaoOptions>()
    .BindConfiguration(AtualizacaoOptions.Secao).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddScoped<AtualizacaoPolicy>();
builder.Services.AddScoped<IPontosService, PontosService>();
builder.Services.AddScoped<IPontoRepository, PontoRepository>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddDbContext<AguaDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var ambiente = sp.GetRequiredService<IHostEnvironment>();
    var connection = new SqliteConnectionStringBuilder(
        config.GetConnectionString("AguaNaRua") ?? "Data Source=agua-na-rua.db;Foreign Keys=True");
    if (connection.DataSource != ":memory:" && !Path.IsPathRooted(connection.DataSource))
        connection.DataSource = Path.GetFullPath(connection.DataSource, ambiente.ContentRootPath);
    options.UseSqlite(connection.ToString());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<ExemplosSchemaFilter>();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Água na Rua — API de C#",
        Version = "v1",
        Description = "CitizenTech no Jardim Peri. Condições relatadas, sem indicação de segurança de passagem. "
            + "Pontos com demonstracao=true e origem DEMONSTRACAO são fictícios. "
            + "RECENTE inclui o limite de 24h, configurável; DESATUALIZADA supera a janela; SEM_OBSERVACAO não tem relato. "
            + "UTC em ISO 8601. Favoritos pertencem à persistência local do mobile."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Água na Rua v1"));
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

if (seedOnly || app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var cancellationToken = app.Lifetime.ApplicationStopping;
    await scope.ServiceProvider.GetRequiredService<AguaDbContext>().Database.MigrateAsync(cancellationToken);
    if (seedOnly || (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DemoSeed:Enabled")))
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().ExecutarAsync(cancellationToken);
}
if (seedOnly) return;
await app.RunAsync();

public partial class Program;
