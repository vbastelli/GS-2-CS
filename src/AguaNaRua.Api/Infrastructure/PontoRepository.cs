using System.Linq.Expressions;
using AguaNaRua.Api.Application;
using AguaNaRua.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AguaNaRua.Api.Infrastructure;

public sealed class PontoRepository(AguaDbContext db) : IPontoRepository
{
    private static readonly Expression<Func<Observacao, ObservacaoResponse>> ProjetarObservacao =
        o => new ObservacaoResponse(o.Id, o.PontoMonitoradoId, o.Condicao, o.Origem, o.ObservadoEm, o.RegistradoEm);

    private sealed record PontoLinha(int Id, string Nome, string Bairro, double Latitude,
        double Longitude, bool Demonstracao, int? UltimaId);

    private static IQueryable<PontoLinha> ProjetarPontos(IQueryable<PontoMonitorado> consulta) => consulta
        .Select(p => new PontoLinha(p.Id, p.Nome, p.Bairro, p.Latitude, p.Longitude, p.Demonstracao,
            p.Observacoes.OrderByDescending(o => o.ObservadoEm)
                .ThenByDescending(o => o.RegistradoEm).ThenByDescending(o => o.Id)
                .Select(o => (int?)o.Id).FirstOrDefault()));

    public async Task<IReadOnlyList<PontoConsulta>> ListarAsync(CancellationToken cancellationToken)
    {
        var pontos = await ProjetarPontos(db.Pontos.AsNoTracking().OrderBy(p => p.Id)).ToListAsync(cancellationToken);
        // Duas consultas no total, independentemente da quantidade de pontos (sem N+1).
        var ids = pontos.Where(p => p.UltimaId.HasValue).Select(p => p.UltimaId!.Value).ToArray();
        var ultimas = await db.Observacoes.AsNoTracking().Where(o => ids.Contains(o.Id))
            .Select(ProjetarObservacao).ToDictionaryAsync(o => o.Id, cancellationToken);
        return pontos.Select(p => Mapear(p, p.UltimaId is int id ? ultimas[id] : null)).ToList();
    }

    public async Task<PontoConsulta?> ObterAsync(int id, CancellationToken cancellationToken)
    {
        var ponto = await ProjetarPontos(db.Pontos.AsNoTracking().Where(p => p.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        if (ponto is null) return null;
        var ultima = ponto.UltimaId is int ultimaId
            ? await ObterObservacaoAsync(id, ultimaId, cancellationToken) : null;
        return Mapear(ponto, ultima);
    }

    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken)
        => db.Pontos.AnyAsync(p => p.Id == id, cancellationToken);

    public async Task<PaginaResponse<ObservacaoResponse>> HistoricoAsync(int id, int page,
        int pageSize, CancellationToken cancellationToken)
    {
        var consulta = db.Observacoes.AsNoTracking().Where(o => o.PontoMonitoradoId == id);
        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta.OrderByDescending(o => o.ObservadoEm)
            .ThenByDescending(o => o.RegistradoEm).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(ProjetarObservacao)
            .ToListAsync(cancellationToken);
        return new PaginaResponse<ObservacaoResponse>(itens, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<ObservacaoResponse?> ObterObservacaoAsync(int id, int observacaoId,
        CancellationToken cancellationToken) => db.Observacoes.AsNoTracking()
        .Where(o => o.PontoMonitoradoId == id && o.Id == observacaoId)
        .Select(ProjetarObservacao).SingleOrDefaultAsync(cancellationToken);

    public async Task AdicionarAsync(Observacao observacao, CancellationToken cancellationToken)
    {
        db.Observacoes.Add(observacao);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static PontoConsulta Mapear(PontoLinha p, ObservacaoResponse? ultima)
        => new(p.Id, p.Nome, p.Bairro, p.Latitude, p.Longitude, p.Demonstracao, ultima);
}
