namespace AguaNaRua.Api.Domain;

public sealed class PontoMonitorado
{
    private readonly List<Observacao> _observacoes = [];

    private PontoMonitorado() { } // Materialização do EF Core.

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Bairro { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool Demonstracao { get; private set; }
    public IReadOnlyCollection<Observacao> Observacoes => _observacoes.AsReadOnly();

    public static PontoMonitorado Criar(string nome, string bairro, double latitude,
        double longitude, bool demonstracao)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 160)
            throw new ValidacaoDominioException("nome", "Informe uma referência de até 160 caracteres.");
        if (string.IsNullOrWhiteSpace(bairro) || bairro.Trim().Length > 100)
            throw new ValidacaoDominioException("bairro", "Informe um bairro de até 100 caracteres.");
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            throw new ValidacaoDominioException("latitude", "Latitude deve estar entre -90 e 90.");
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new ValidacaoDominioException("longitude", "Longitude deve estar entre -180 e 180.");

        return new PontoMonitorado
        {
            Nome = nome.Trim(),
            Bairro = bairro.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            Demonstracao = demonstracao
        };
    }
}
