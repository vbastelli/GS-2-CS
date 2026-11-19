namespace AguaNaRua.ConsoleApp;

public sealed record ConfiguracaoConsole(Uri ApiUrl, bool Ajuda = false)
{
    public const string UrlPadrao = "http://localhost:5080";

    public static ConfiguracaoConsole Ler(string[] args)
    {
        if (args is ["--help"] or ["-h"]) return new(new Uri(UrlPadrao), true);
        var endereco = args.Length == 0 ? UrlPadrao
            : args is ["--api-url", var valor] ? valor
            : throw new ArgumentException("Use --api-url <endereço> ou --help.");

        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host) || uri.Port < 1
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("A URL da API deve ser absoluta, HTTP ou HTTPS, sem credenciais, query ou fragmento.");

        return new(new Uri(uri.AbsoluteUri.TrimEnd('/') + "/"));
    }
}
