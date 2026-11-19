using System.Text;
using AguaNaRua.ConsoleApp;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = new UTF8Encoding(false);

ConfiguracaoConsole configuracao;
try { configuracao = ConfiguracaoConsole.Ler(args); }
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Uso: dotnet run --project src/AguaNaRua.Console -- --api-url http://localhost:5080");
    return 2;
}
if (configuracao.Ajuda)
{
    Console.WriteLine("Água na Rua — cliente HTTP da Web API.");
    Console.WriteLine("Uso: dotnet run --project src/AguaNaRua.Console -- [--api-url http://localhost:5080]");
    Console.WriteLine("Timeout HTTP: 10 segundos. Opção 0 ou Ctrl+C para sair.");
    return 0;
}

using var cancelamento = new CancellationTokenSource();
ConsoleCancelEventHandler aoCancelar = (_, evento) => { evento.Cancel = true; cancelamento.Cancel(); };
Console.CancelKeyPress += aoCancelar;
try
{
    using var http = new HttpClient { BaseAddress = configuracao.ApiUrl, Timeout = TimeSpan.FromSeconds(10) };
    http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    // StreamReader assíncrono evita o ReadLine síncrono do wrapper Console.In.
    using var entrada = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8,
        detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
    await new MenuConsole(new AguaNaRuaClient(http), entrada, Console.Out, TimeProvider.System)
        .ExecutarAsync(cancelamento.Token);
    return 0;
}
catch (IOException)
{
    Console.Error.WriteLine("A entrada ou saída do terminal foi encerrada. Finalizando a aplicação.");
    return 1;
}
finally { Console.CancelKeyPress -= aoCancelar; }
