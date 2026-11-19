# Evidências da alteração do Console — 19/11/2026

Estas evidências pertencem à inclusão de `AguaNaRua.Console`. Os registros da implementação inicial em [../README.md](../README.md), de 18/11/2026, foram preservados. A data UTC exata da execução mais recente, a URL e os resultados estão em [verificacao.json](verificacao.json).

Ambiente executado: Windows, SDK **10.0.200**, framework **net10.0**, EF Core **10.0.4** e SQLite em arquivo novo e isolado, ignorado pelo Git. O Console não referencia o projeto da API nem seu banco: utiliza HTTP. Os pontos e relatos gerados nesta execução são fictícios, sem validação em campo. Relatos do seed têm origem `DEMONSTRACAO`; os criados pelo endpoint público têm `RELATO_CIDADAO`.

## Reproduzir a verificação

Na raiz, em Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verificar-Console.ps1
```

O script usa `http://localhost:5082`, banco novo em `tmp/` e dois processos reais: API e Console. Cada cenário fornece entradas ao Console pela entrada padrão e captura sua saída; somente espaços finais de apresentação são removidos dos logs. Não são respostas de menu reconstruídas. Os arquivos `*.entrada.txt` registram somente as escolhas e valores usados no teste; linhas vazias são intencionais. Ao final, o script para a API que iniciou e testa a falha de conexão. Aceita `-Porta 5083` se a porta estiver ocupada.

## Build, testes e banco

| Verificação | Resultado | Saída real |
|---|---|---|
| `dotnet tool restore` | ExitCode 0 | [ferramentas.txt](ferramentas.txt) |
| `dotnet restore AguaNaRua.sln --locked-mode` | ExitCode 0 | [restore.txt](restore.txt) |
| `dotnet build AguaNaRua.sln --no-restore` | Zero avisos, zero erros | [build.txt](build.txt) |
| `dotnet test AguaNaRua.sln --no-build --no-restore` | **83 aprovados**, zero falhas ou ignorados | [testes.txt](testes.txt) |
| Migration em arquivo SQLite novo | ExitCode 0 | [migrations.txt](migrations.txt) |

Os 83 testes incluem os 49 anteriores da API e 34 novos casos do Console. Os novos testes cobrem configuração, validação de contratos, payload de somente dois campos, enums em texto, instantes com offset, ProblemDetails, JSON incompatível, lista vazia, entradas inválidas, EOF, timeout, cancelamento, navegação entre páginas e integração do menu com a API e SQLite reais. Timeout e cancelamento cooperativo são exercitados nos testes automatizados; não foram simulados por respostas fictícias da API nas transcrições dos processos.

## Cenários do Console em processo real

| Cenário | Resultado conferido | Saída / entrada |
|---|---|---|
| Menu completo | Três pontos e estados; detalhes e coordenadas; ponto sem observação; histórico vazio e com relato; POST 201 seguido de consulta; opções e IDs inválidos; HTTP 400/404; Sair com código 0 | [saída](01-menu-completo.txt), [entrada](01-menu-completo.entrada.txt) |
| Horário manual com offset | Formato inválido recusado; ISO 8601 com `-03:00` aceito; mesmo instante persistido e exibido em UTC; consulta do histórico após criação | [saída](02-horario-com-offset.txt), [entrada](02-horario-com-offset.entrada.txt), [relato persistido](relato-com-offset.json) |
| Histórico paginado | Página 1 → 2 → 1 → 2; tentativa de avançar além da última página informada como inválida; retorno ao menu e Sair | [saída](03-paginacao.txt), [entrada](03-paginacao.entrada.txt) |
| Fim da entrada padrão | EOF enquanto solicita ID; encerramento normal, código 0 | [saída](04-fim-da-entrada.txt), [entrada](04-fim-da-entrada.entrada.txt) |
| URL inválida | `ftp://localhost` recusada antes de acessar a rede; código 2 e instrução de uso | [saída](05-url-invalida.txt) |
| Ajuda | Argumentos, URL e timeout apresentados; código 0 | [saída](06-ajuda.txt) |
| API desligada | Endereço e comando de inicialização apresentados; retorno ao menu sem tentativas automáticas; opção 0 encerra com código 0 | [saída](07-api-desligada.txt), [entrada](07-api-desligada.entrada.txt) |

O [JSON inicial](pontos-iniciais.json) comprova `RECENTE`, `DESATUALIZADA` e `SEM_OBSERVACAO`. O [relato criado e consultado](relato-criado-e-consultado.json) foi obtido por GET após o POST do Console. A API foi acessada pela porta alternativa por meio de `--api-url`; o banco padrão do projeto não foi alterado por esse script.

Também foi executado o comando documentado **sem `--no-build`**, em terminal interativo: `dotnet run --project src/AguaNaRua.Console -- --api-url http://localhost:5082`. As escolhas `1`, `2`, ID `3` e `0` mostraram a listagem, o ponto sem observação e o encerramento com código 0: [transcrição do terminal](terminal-interativo.txt), com sequências de controle do terminal removidas.

Os comandos de preparação do README foram conferidos com outro arquivo SQLite: [migration e seed explícito](preparacao-readme.txt). A API iniciou com `dotnet run --project src/AguaNaRua.Api --launch-profile http -- --urls http://localhost:5082`. O [exemplo PowerShell com data dinâmica](comandos-http-readme.json) também criou e consultou um relato nesse banco. `dotnet run --project src/AguaNaRua.Console` foi executado com URL padrão e encerrado pela opção 0; `dotnet run --project src/AguaNaRua.Console -- --help` apresentou a ajuda.

Trechos da saída real:

```text
Demonstração: SIM — referência fictícia, não validada em campo
Estado de atualização: SEM_OBSERVACAO
SEM_OBSERVACAO — nenhum relato disponível; ausência de informação.
Observação registrada com sucesso (HTTP 201).
Origem: RELATO_CIDADAO
HTTP 400 — Dados recusados pela API.
observadoEm: O horário observado não pode estar no futuro.
Até logo!
```

As linhas acima são trechos de momentos distintos do [cenário completo](01-menu-completo.txt). Os horários completos estão nas transcrições, com `Z (UTC)`; não foram substituídos por datas de exemplo.

## Outras formas de reprodução conferidas

Swagger UI e OpenAPI retornaram **HTTP 200** nesta execução. O exemplo PowerShell do README usou data dinâmica, fez POST e consultou o ID retornado: [exemplo-http-readme.json](exemplo-http-readme.json). O arquivo `.http` existente foi preservado, incluindo sua variável de data dinâmica e o GET pelo ID da resposta.

As formas A, B e C, incluindo preparação, comandos e solução de problemas, estão no [README principal](../../../README.md#formas-de-reprodução-para-o-professor). A captura gráfica do Swagger não foi repetida nesta alteração. A verificação foi feita por HTTP e pelo Console real.
