using AguaNaRua.Api.Application;
using AguaNaRua.Api.Domain;
using Microsoft.Extensions.Options;

namespace AguaNaRua.Tests;

public sealed class DomainTests
{
    private readonly AtualizacaoPolicy _policy = new(new FakeClock(), Options.Create(new AtualizacaoOptions()));

    [Fact]
    public void Sem_observacao_representa_ausencia_de_informacao()
        => Assert.Equal(EstadoAtualizacao.SEM_OBSERVACAO, _policy.Classificar(null));

    [Theory]
    [InlineData(-1, EstadoAtualizacao.RECENTE)]
    [InlineData(0, EstadoAtualizacao.RECENTE)]
    [InlineData(1, EstadoAtualizacao.DESATUALIZADA)]
    public void Limite_de_24_horas_e_inclusivo_em_precisao_de_ticks(long ticksAposLimite, EstadoAtualizacao esperado)
        => Assert.Equal(esperado, _policy.Classificar(FakeClock.Instante.AddHours(-24).AddTicks(-ticksAposLimite)));

    [Fact]
    public void Janela_pode_ser_configurada()
    {
        var policy = new AtualizacaoPolicy(new FakeClock(), Options.Create(new AtualizacaoOptions { JanelaHoras = 1 }));
        Assert.Equal(EstadoAtualizacao.DESATUALIZADA, policy.Classificar(FakeClock.Instante.AddHours(-2)));
    }

    [Fact]
    public void Rejeita_horario_futuro_mesmo_por_um_tick()
        => Assert.Throws<ValidacaoDominioException>(() => Observacao.Criar(1, CondicaoRelatada.AGUA_ACUMULADA,
            OrigemObservacao.RELATO_CIDADAO, FakeClock.Instante.AddTicks(1), FakeClock.Instante));

    [Fact]
    public void Normaliza_offset_sem_alterar_instante()
    {
        var observado = FakeClock.Instante.ToOffset(TimeSpan.FromHours(-3));
        var observacao = Observacao.Criar(1, CondicaoRelatada.AGUA_ACUMULADA,
            OrigemObservacao.RELATO_CIDADAO, observado, FakeClock.Instante);
        Assert.Equal(TimeSpan.Zero, observacao.ObservadoEm.Offset);
        Assert.Equal(FakeClock.Instante, observacao.ObservadoEm);
    }

    [Fact]
    public void Rejeita_condicao_fora_do_enum()
        => Assert.Throws<ValidacaoDominioException>(() => Observacao.Criar(1, (CondicaoRelatada)99,
            OrigemObservacao.RELATO_CIDADAO, FakeClock.Instante, FakeClock.Instante));

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void Rejeita_coordenadas_invalidas(double latitude, double longitude)
        => Assert.Throws<ValidacaoDominioException>(() => PontoMonitorado.Criar("A", "Jardim Peri", latitude, longitude, true));

    [Fact]
    public void Referencias_obrigatorias_nao_podem_ser_vazias()
        => Assert.Throws<ValidacaoDominioException>(() => PontoMonitorado.Criar(" ", "Jardim Peri", 0, 0, true));
}
