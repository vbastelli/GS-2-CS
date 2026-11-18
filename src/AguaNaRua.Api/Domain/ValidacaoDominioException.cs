namespace AguaNaRua.Api.Domain;

public sealed class ValidacaoDominioException(string campo, string mensagem) : Exception(mensagem)
{
    public string Campo { get; } = campo;
}
