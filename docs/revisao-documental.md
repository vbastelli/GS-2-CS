# Revisão documental da entrega de C# — 19/11/2026

## Escopo e versão conferida

Auditoria de documentação sobre o commit `20f6a44906a4f5b30dd6b85dba0bf303245e6f8f`, que contém a API e o Console. A branch remota `main` foi consultada e apontava para o mesmo commit no início da verificação. A auditoria foi inicialmente preparada sem commit ou push; posteriormente, a equipe autorizou o versionamento e a publicação das alterações documentais.

Alterações documentais: [README principal](../README.md), [Ficha da CitizenTech](agua-na-rua.md) e este registro. Código, solution, configurações, testes, migrations, scripts e evidências históricas foram preservados. Foram conferidos os hashes SHA-256 dos 105 arquivos versionados fora dos dois documentos editados; nenhum deles foi alterado.

## Correções e verificações

| Item | Verificação realizada e resultado |
|---|---|
| Instruções do repositório | Busca por `AGENTS.md` no repositório e diretórios ancestrais aplicáveis; nenhum arquivo foi encontrado. |
| Independência de C# | Ficha e README descrevem Web API com código, banco, migrations, testes e configuração próprios. SOA permanece uma entrega separada. A descrição antiga foi substituída. |
| Tabela de problemas | Linhas soltas reunidas em uma tabela; duplicação do exemplo de outra porta removida. Orientações de banco, seed, conexão, Swagger e Console preservadas. |
| Reprodução | Formas A (Swagger), B (Console em segundo terminal) e C (`.http`/PowerShell) mantidas, com preparação comum e datas dinâmicas nos exemplos de criação. |
| Comandos e configurações | Caminhos dos três projetos, solution, ferramenta EF, argumentos `--seed`, `--api-url`, `--help` e `--urls` conferidos contra a estrutura e o código. Perfil `http` e porta 5080 conferidos em `launchSettings.json`; SQLite, seed e janela de 24h conferidos nas configurações e implementação. |
| Shell | Blocos com variáveis de ambiente, `Invoke-RestMethod`, `ConvertTo-Json`, `Remove-Item` e scripts `.ps1` identificados como PowerShell. Comandos `dotnet` comuns indicados como válidos em PowerShell ou bash. |
| Links e tabelas | Verificados 5 documentos Markdown, 95 links locais/âncoras e 17 tabelas, sem erros. Conferidas 17 referências a caminhos de projetos no README e a sintaxe de 7 blocos PowerShell com o parser do shell. |
| Histórico de verificações | Separadas as etapas com 49 testes em 18/11 e 83 testes na inclusão do Console em 19/11. Os arquivos anteriores de saída não foram regravados. |
| Declarações de conclusão | Execução técnica sustentada por código e testes; validação de campo, medições e envio acadêmico continuam sem comprovação fornecida. |

## Equipe e turma

Os quatro nomes e RMs coincidem entre o README e [integrantes.txt](integrantes.txt):

| Nome | RM |
|---|---|
| Lorenzo Hayashi Mangini | 554901 |
| Milton Cezar Bacanieski | 555206 |
| Vitor Bebiano Mulford | 555026 |
| Victório Maia Bastelli | 554723 |

A turma **3ESPZ** foi fornecida pela equipe na solicitação desta revisão. O TXT de integrantes não informa turma; a busca textual nas 40 páginas do edital também não encontrou `3ESPZ`. A documentação identifica essa origem, sem atribuir ao PDF ou ao TXT uma confirmação inexistente. O TXT não foi alterado.

## Resultados técnicos reconferidos

As verificações abaixo foram executadas nesta auditoria sobre a implementação existente, sem alterações funcionais:

| Comando | Resultado desta revisão |
|---|---|
| `dotnet restore AguaNaRua.sln --locked-mode` | ExitCode 0; arquivos de versões fixadas preservados. |
| `dotnet build AguaNaRua.sln --no-restore` | Falhou com MSB3027/MSB3021: o executável Debug da API estava bloqueado por uma instância em execução. Não foi registrado como build aprovado. |
| `dotnet build AguaNaRua.sln --no-restore --configuration Release` | ExitCode 0; zero avisos e zero erros. |
| `dotnet test AguaNaRua.sln --no-build --no-restore --configuration Release --logger 'console;verbosity=minimal'` | ExitCode 0; **83 aprovados, zero falhas e zero ignorados**. |

Trechos reais da execução Release, sem caminhos do perfil:

```text
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)

Aprovado!  – Com falha:     0, Aprovado:    83, Ignorado:     0, Total:    83
```

O bloqueio do executável Debug é uma condição de execução no Windows, não uma falha funcional identificada na API. O processo existente foi preservado e o README orienta encerrar API/Console antes de recompilar a configuração em uso. Não foi identificada falha funcional concreta nos testes executados. Os fluxos HTTP e do menu não foram repetidos nesta auditoria documental: as respectivas evidências permanecem nos registros de [18/11](evidencias/README.md) e da [inclusão do Console em 19/11](evidencias/console/README.md).

## Pendências reais e limites

- Não foram fornecidos registros de entrevistas, pesquisa com moradores, validação atual dos pontos ou medições de impacto. A proposta continua situada no Jardim Peri, e os pontos do protótipo continuam fictícios.
- A turma foi informada pela equipe; não há confirmação da turma nos documentos acadêmicos fornecidos.
- Não foi fornecido comprovante de envio no Teams/Portal do Aluno. O edital, páginas 23 e 30, define o envio do link do GitHub por um integrante; esta revisão não confirma esse envio.
- Contrato definitivo com o mobile e eventuais complementos do professor permanecem dependentes da equipe. O contrato HTTP desta API está documentado no Swagger.
- Não foram reexecutados testes de vulnerabilidade, pesquisa externa ou captura gráfica. Os resultados anteriores são apresentados como históricos.
- A publicação desta revisão documental foi autorizada pela equipe após a auditoria; isso não constitui comprovante de envio acadêmico.
