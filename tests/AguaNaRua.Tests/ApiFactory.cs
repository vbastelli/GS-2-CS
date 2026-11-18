using System.Data.Common;
using AguaNaRua.Api.Domain;
using AguaNaRua.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AguaNaRua.Tests;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"agua-na-rua-tests-{Guid.NewGuid():N}.db");
    public ConsultaCounter Counter { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AguaNaRua"] = $"Data Source={DatabasePath};Foreign Keys=True;Pooling=False",
            ["Database:ApplyMigrations"] = "true",
            ["DemoSeed:Enabled"] = "false",
            ["Logging:LogLevel:Default"] = "Warning"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FakeClock());
            services.ConfigureDbContext<AguaDbContext>(options => options.AddInterceptors(Counter));
        });
    }

    public async Task<int> CriarPontoAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AguaDbContext>();
        var ponto = PontoMonitorado.Criar("Teste fictício", "Jardim Peri", -23.445, -46.657, true);
        db.Pontos.Add(ponto);
        await db.SaveChangesAsync();
        return ponto.Id;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (File.Exists(DatabasePath)) File.Delete(DatabasePath);
    }
}

internal sealed class ConsultaCounter : DbCommandInterceptor
{
    public List<string> Comandos { get; } = [];

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Comandos.Add(command.CommandText);
        return ValueTask.FromResult(result);
    }
}
