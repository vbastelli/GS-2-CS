namespace AguaNaRua.Api.Domain;

public sealed class Observacao
{
    private Observacao() { }

    public int Id { get; private set; }
    public int PontoMonitoradoId { get; private set; }
    public PontoMonitorado PontoMonitorado { get; private set; } = null!;
    public CondicaoRelatada Condicao { get; private set; }
    public OrigemObservacao Origem { get; private set; }
    public DateTimeOffset ObservadoEm { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }

    public static Observacao Criar(int pontoId, CondicaoRelatada condicao,
        OrigemObservacao origem, DateTimeOffset observadoEm, DateTimeOffset registradoEm)
    {
        if (pontoId <= 0)
            throw new ValidacaoDominioException("id", "O identificador do ponto deve ser positivo.");
        if (!Enum.IsDefined(condicao))
            throw new ValidacaoDominioException("condicao", "Condição relatada inválida.");
        if (!Enum.IsDefined(origem))
            throw new ValidacaoDominioException("origem", "Origem inválida.");
        if (observadoEm == default)
            throw new ValidacaoDominioException("observadoEm", "Informe o horário observado.");
        if (observadoEm > registradoEm)
            throw new ValidacaoDominioException("observadoEm", "O horário observado não pode estar no futuro.");

        return new Observacao
        {
            PontoMonitoradoId = pontoId,
            Condicao = condicao,
            Origem = origem,
            ObservadoEm = observadoEm.ToUniversalTime(),
            RegistradoEm = registradoEm.ToUniversalTime()
        };
    }
}
