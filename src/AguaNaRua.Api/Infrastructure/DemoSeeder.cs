using AguaNaRua.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AguaNaRua.Api.Infrastructure;

public sealed class DemoSeeder(AguaDbContext db, TimeProvider relogio)
{
    public async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        // Toda a carga é atômica. Banco já iniciado nunca recebe relatos renovados artificialmente.
        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.Pontos.AnyAsync(cancellationToken)) return;

        var pontos = new[]
        {
            PontoMonitorado.Criar("Trecho A — demonstração fictícia", "Jardim Peri", -23.4450, -46.6570, true),
            PontoMonitorado.Criar("Trecho B — demonstração fictícia", "Jardim Peri", -23.4460, -46.6580, true),
            PontoMonitorado.Criar("Trecho C — demonstração fictícia", "Jardim Peri", -23.4470, -46.6590, true)
        };
        db.Pontos.AddRange(pontos);
        await db.SaveChangesAsync(cancellationToken);

        var agora = relogio.GetUtcNow();
        db.Observacoes.AddRange(
            Observacao.Criar(pontos[0].Id, CondicaoRelatada.AGUA_ACUMULADA,
                OrigemObservacao.DEMONSTRACAO, agora.AddMinutes(-30), agora),
            Observacao.Criar(pontos[1].Id, CondicaoRelatada.ALAGAMENTO_RELATADO,
                OrigemObservacao.DEMONSTRACAO, agora.AddHours(-48), agora));
        await db.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }
}
