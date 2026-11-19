using System.Globalization;

namespace AguaNaRua.ConsoleApp;

public sealed class MenuConsole(AguaNaRuaClient api, TextReader entrada, TextWriter saida, TimeProvider relogio)
{
    public async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        saida.WriteLine($"API: {api.Endereco}");
        saida.WriteLine("Horários exibidos em UTC. Os estados de atualização são informados pela API.");
        saida.WriteLine("RECENTE: dentro da janela configurada; DESATUALIZADA: fora da janela; SEM_OBSERVACAO: ausência de informação.");
        saida.WriteLine("Condições relatadas não indicam segurança de passagem. Pontos e relatos fictícios são identificados.");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                saida.WriteLine();
                saida.WriteLine("Água na Rua");
                saida.WriteLine("1. Listar pontos monitorados");
                saida.WriteLine("2. Consultar detalhes de um ponto");
                saida.WriteLine("3. Consultar histórico de observações");
                saida.WriteLine("4. Registrar uma observação");
                saida.WriteLine("0. Sair");
                var opcao = await LerAsync("Escolha uma opção: ", cancellationToken);
                if (opcao == "0") { saida.WriteLine("Até logo!"); return; }
                try
                {
                    switch (opcao)
                    {
                        case "1": await ListarAsync(cancellationToken); break;
                        case "2": await ConsultarAsync(cancellationToken); break;
                        case "3": await HistoricoAsync(cancellationToken); break;
                        case "4": await RegistrarAsync(cancellationToken); break;
                        default: saida.WriteLine("Opção inválida. Escolha 1, 2, 3, 4 ou 0."); break;
                    }
                }
                catch (RespostaApiException ex) { saida.WriteLine(ex.Message); }
                catch (HttpRequestException) { InformarApiIndisponivel(); }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    saida.WriteLine($"Tempo limite de {api.Timeout.TotalSeconds:g} segundos ao acessar {api.Endereco}. Nenhuma tentativa automática será feita.");
                    InformarComandoApi();
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (FimDaEntradaException) { saida.WriteLine("Entrada encerrada. Até logo!"); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            saida.WriteLine("Operação cancelada. Encerrando Água na Rua.");
        }
    }

    private async Task ListarAsync(CancellationToken cancellationToken)
    {
        var pontos = await api.ListarAsync(cancellationToken);
        if (pontos.Count == 0) { saida.WriteLine("Nenhum ponto monitorado cadastrado. Prepare os dados de demonstração na API."); return; }
        foreach (var ponto in pontos) ExibirPonto(ponto, detalhes: false);
    }

    private async Task ConsultarAsync(CancellationToken cancellationToken)
    {
        var id = await LerIdAsync(cancellationToken);
        ExibirPonto(await api.ObterAsync(id, cancellationToken), detalhes: true);
    }

    private async Task HistoricoAsync(CancellationToken cancellationToken)
    {
        var id = await LerIdAsync(cancellationToken);
        var page = 1;
        while (true)
        {
            var pagina = await api.HistoricoAsync(id, page, cancellationToken);
            if (pagina.TotalItens == 0)
            {
                saida.WriteLine($"Histórico do ponto {id}:");
                saida.WriteLine("Sem observações: ausência de informação sobre este ponto.");
                return;
            }
            saida.WriteLine($"Histórico do ponto {id} — página {pagina.Page} de {pagina.TotalPaginas}, {pagina.TotalItens} observações.");
            foreach (var observacao in pagina.Itens) ExibirObservacao(observacao);
            while (true)
            {
                var escolha = await LerAsync("N: próxima página | A: anterior | 0: voltar ao menu: ", cancellationToken);
                if (escolha == "0") return;
                if (escolha.Equals("n", StringComparison.OrdinalIgnoreCase) && page < pagina.TotalPaginas) { page++; break; }
                if (escolha.Equals("a", StringComparison.OrdinalIgnoreCase) && page > 1) { page--; break; }
                saida.WriteLine("Navegação inválida ou página indisponível.");
            }
        }
    }

    private async Task RegistrarAsync(CancellationToken cancellationToken)
    {
        var id = await LerIdAsync(cancellationToken);
        saida.WriteLine("Condição relatada:");
        saida.WriteLine("1. Sem água acumulada observada");
        saida.WriteLine("2. Água acumulada");
        saida.WriteLine("3. Alagamento relatado");
        CondicaoRelatada condicao;
        while (true)
        {
            var escolha = await LerAsync("Escolha a condição (1 a 3): ", cancellationToken);
            if (escolha is "1" or "2" or "3") { condicao = (CondicaoRelatada)(int.Parse(escolha, CultureInfo.InvariantCulture) - 1); break; }
            saida.WriteLine("Condição inválida. Escolha 1, 2 ou 3.");
        }

        DateTimeOffset observadoEm;
        while (true)
        {
            var escolha = await LerAsync("Horário: 1. Agora (UTC) | 2. Digitar ISO 8601: ", cancellationToken);
            if (escolha == "1") { observadoEm = relogio.GetUtcNow(); break; }
            if (escolha == "2")
            {
                var texto = await LerAsync("Horário observado (ISO 8601 com Z ou offset, incluindo segundos): ", cancellationToken);
                if (HorarioIso8601.TentarLer(texto, out observadoEm)) break;
                saida.WriteLine("Horário inválido. Informe data e hora com Z ou offset explícito, por exemplo AAAA-MM-DDTHH:mm:ss-03:00.");
            }
            else saida.WriteLine("Opção de horário inválida. Escolha 1 ou 2.");
        }

        var criado = await api.RegistrarAsync(id, new RegistrarObservacaoDto(condicao, observadoEm), cancellationToken);
        saida.WriteLine("Observação registrada com sucesso (HTTP 201).");
        ExibirObservacao(criado);
    }

    private async Task<int> LerIdAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var texto = await LerAsync("ID do ponto: ", cancellationToken);
            if (int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0) return id;
            saida.WriteLine("ID inválido. Informe um número inteiro positivo.");
        }
    }

    private async ValueTask<string> LerAsync(string mensagem, CancellationToken cancellationToken)
    {
        saida.Write(mensagem);
        var linha = await entrada.ReadLineAsync(cancellationToken).AsTask().WaitAsync(cancellationToken);
        if (linha is null) throw new FimDaEntradaException();
        return linha.Trim();
    }

    private void ExibirPonto(PontoDto ponto, bool detalhes)
    {
        saida.WriteLine($"\nID: {ponto.Id} | {ponto.Nome} | Bairro: {ponto.Bairro}");
        saida.WriteLine($"Demonstração: {(ponto.Demonstracao ? "SIM — referência fictícia, não validada em campo" : "não")}");
        saida.WriteLine($"Estado de atualização: {ponto.StatusAtualizacao}");
        if (detalhes) saida.WriteLine(FormattableString.Invariant($"Coordenadas: latitude {ponto.Latitude}, longitude {ponto.Longitude}"));
        if (ponto.UltimaObservacao is { } observacao)
        {
            saida.WriteLine("Última observação:");
            ExibirObservacao(observacao);
        }
        else saida.WriteLine("SEM_OBSERVACAO — nenhum relato disponível; ausência de informação.");
    }

    private void ExibirObservacao(ObservacaoDto observacao)
    {
        var condicao = observacao.Condicao switch
        {
            CondicaoRelatada.SEM_AGUA_ACUMULADA_OBSERVADA => "Sem água acumulada observada",
            CondicaoRelatada.AGUA_ACUMULADA => "Água acumulada",
            CondicaoRelatada.ALAGAMENTO_RELATADO => "Alagamento relatado",
            _ => observacao.Condicao.ToString()
        };
        saida.WriteLine($"Observação ID: {observacao.Id} | Ponto: {observacao.PontoId} | Condição: {condicao} ({observacao.Condicao})");
        saida.WriteLine($"Observado em: {HorarioIso8601.Exibir(observacao.ObservadoEm)}");
        saida.WriteLine($"Registrado em: {HorarioIso8601.Exibir(observacao.RegistradoEm)}");
        saida.WriteLine($"Origem: {observacao.Origem}{(observacao.Origem == OrigemObservacao.DEMONSTRACAO ? " — relato fictício de demonstração" : "")}");
    }

    private void InformarApiIndisponivel()
    {
        saida.WriteLine($"Não foi possível acessar a API em {api.Endereco}. Verifique a URL e se a API está em execução.");
        InformarComandoApi();
    }

    private void InformarComandoApi()
        => saida.WriteLine("Para iniciar a API na raiz do repositório: dotnet run --project src/AguaNaRua.Api --launch-profile http");

    private sealed class FimDaEntradaException : Exception;
}
