using System.ComponentModel.DataAnnotations;

namespace AguaNaRua.Api.Application;

public sealed class AtualizacaoOptions
{
    public const string Secao = "Atualizacao";

    [Range(1, 8760)]
    public int JanelaHoras { get; set; } = 24;
}

public sealed class AtualizacaoPolicy(TimeProvider relogio,
    Microsoft.Extensions.Options.IOptions<AtualizacaoOptions> options)
{
    public EstadoAtualizacao Classificar(DateTimeOffset? observadoEm)
    {
        if (observadoEm is null) return EstadoAtualizacao.SEM_OBSERVACAO;
        return relogio.GetUtcNow() - observadoEm.Value <= TimeSpan.FromHours(options.Value.JanelaHoras)
            ? EstadoAtualizacao.RECENTE
            : EstadoAtualizacao.DESATUALIZADA;
    }
}
