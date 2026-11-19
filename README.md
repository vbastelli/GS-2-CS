# Ficha da CitizenTech — Água na Rua

- **Nome:** Água na Rua.
- **Turma:** 3ESPZ, informada pela equipe na solicitação desta revisão documental; o TXT de integrantes e o edital fornecido não especificam a turma.
- **Cidade e bairro:** São Paulo — Jardim Peri, com seleção de ruas e pontos específicos após validação com moradores.
- **Cidadão beneficiado:** moradores que precisarão circular por trechos sujeitos a alagamento, especialmente ao sair ou retornar do trabalho.
- **Problema em uma frase:** quem depende de ruas sujeitas a alagamento enfrenta dificuldade para encontrar informações recentes e localizadas sobre água acumulada no seu caminho.
- **Como funcionará:** o cidadão consultará pontos acompanhados, visualizará a última observação com horário e origem, salvará favoritos e poderá registrar uma nova ocorrência.
- **O que cada parte fará:** o software organizará as observações; o ML investigará padrões históricos de alagamento; o circuito demonstrará monitoramento local da elevação da água; infraestrutura, segurança e testes sustentarão a proposta.
- **Indicador de impacto:** será medido o tempo para encontrar a informação mais recente sobre um trecho. Também será avaliado se o usuário identificará corretamente sua origem, atualização e eventual ausência de informação. Não foram fornecidas evidências dessas medições.
- **ODS relacionados:** **ODS 11, meta 11.5**, pela contribuição ao acesso à informação sobre desastres relacionados à água; **ODS 13, meta 13.1**, pelo apoio à preparação e adaptação comunitária. O protótipo não comprova redução de danos ou vítimas.
- **Link do repositório:** [github.com/vbastelli/GS-2-CS](https://github.com/vbastelli/GS-2-CS).

O grupo propõe um protótipo para organizar informações de aproximadamente três pontos em um pequeno território. O Jardim Peri será investigado por possuir histórico documentado de problemas de drenagem. Como houve intervenções na região, a equipe ainda verificará se os trechos escolhidos apresentam uma necessidade relevante de acompanhamento, por conversas com moradores e investigação das condições atuais.

Cada observação mostra local, horário, origem e condição relatada. O aplicativo distingue informação recente, informação antiga e **ausência de informação**. A falta de registros não garante passagem livre. A solução não recomenda atravessar áreas alagadas e não emite alertas operacionais.

As disciplinas compartilham o problema, o cidadão e o conceito. **A Web API de C# é uma entrega independente e autocontida**, com código, banco, migrations, testes e configuração próprios, conforme a [Ficha da CitizenTech](docs/agua-na-rua.md). **SOA e Web Services é uma entrega separada**, com backend próprio.

| Disciplina | Participação na proposta |
|---|---|
| SOA e Web Services | Backend próprio da disciplina, fora deste repositório de C#. |
| C# Software Development | Web API independente em .NET, com código, banco, migrations e testes próprios; POO, interfaces, DI, EF Core, LINQ e async/await. O Console é seu cliente HTTP. |
| IA & Machine Learning | Notebook independente: KMeans + PCA para investigar recorrência e sazonalidade de ocorrências públicas e apoiar a seleção de pontos. |
| Mobile Development & IoT | React Native, TypeScript, Expo e Expo Router; consulta e registro pela API ou mock identificado; **favoritos na persistência SQLite local do mobile**. |
| Operating Systems | VM Linux por Shell Script, serviços, página de acompanhamento, monitoramento, logs e recuperação. Entrega própria. |
| Physical Computing | Wokwi com ESP32, ultrassônico, display e sinalização; simulação da elevação da água. Entrega própria. |
| Cybersecurity | Análise de PII, taint analysis, tratamento de saídas, explicação por LLM e painel de Risco Cidadão. Entrega própria. |
| Testing, Compliance & QA | Features, critérios de aceite, Test Cases no Azure Boards, evidências em PDF e pitch. Entrega própria. |

O ML pretende explorar [ocorrências de alagamento do GeoSampa](https://metadados.geosampa.prefeitura.sp.gov.br/geonetwork/srv/api/records/432c06b1-03a1-4b1f-9210-423e1b58e869), com recorte 2013–2025 sujeito à verificação de cobertura e consistência. A ficha prevê avaliar ao menos dez combinações de três hiperparâmetros, com validação no treino e avaliação separada. Isso não compõe o runtime desta API e não promete prever a próxima enchente. Sensores instalados, transmissão automática e notificações são evoluções futuras.

## Integrantes

Os quatro nomes e RMs abaixo foram conferidos com [docs/integrantes.txt](docs/integrantes.txt). A turma **3ESPZ** foi informada pela equipe nesta revisão; não consta no TXT nem no edital fornecido.

| Nome | RM |
|---|---|
| Lorenzo Hayashi Mangini | 554901 |
| Milton Cezar Bacanieski | 555206 |
| Vitor Bebiano Mulford | 555026 |
| Victório Maia Bastelli | 554723 |

## Backend demonstrável

ASP.NET Core Web API com Controllers, dois modelos centrais (`PontoMonitorado` e `Observacao`) e DTOs HTTP separados. Não importa nem integra código, banco, projetos ou endpoints de SOA. Funciona sem ML, mobile, Wokwi, VM ou serviços externos.

O projeto [AguaNaRua.Console](src/AguaNaRua.Console/) oferece um menu em português para demonstrar a API pelo terminal. É um cliente HTTP independente: regras de negócio, validações dos relatos, EF Core e persistência continuam na API. Veja as [três formas de reprodução para o professor](#formas-de-reprodução-para-o-professor).

O seed contém **três referências e coordenadas fictícias para demonstração**, identificadas com `demonstracao: true`. Não representam pontos piloto validados nem dados oficiais. Os dois relatos de exemplo têm `origem: DEMONSTRACAO`. Relatos recebidos pelo endpoint público têm `origem: RELATO_CIDADAO`, inclusive quando associados a um ponto demonstrativo. Não há nomes, contatos ou campo de texto livre nos relatos.

## Pré-requisitos e versões

- [.NET SDK 10.0.200](https://dotnet.microsoft.com/download/dotnet/10.0), LTS, fixado em `global.json` (aceita patches da mesma feature band). Versão efetivamente executada: **10.0.200**; runtime **10.0.4**.
- EF Core SQLite, Design e ferramenta `dotnet-ef`: **10.0.4**. O SQLite é embarcado; não requer instalação de servidor nem CLI `sqlite3`.
- Swashbuckle **10.0.1**, Microsoft.OpenApi **2.7.5**, SQLitePCLRaw bundle **2.1.14**. As referências explícitas das duas últimas evitam as dependências transitivas vulneráveis apontadas no primeiro restore. Não foram suprimidos os avisos do NuGet.
- xUnit **2.9.3**, runner **3.1.4**, Microsoft.NET.Test.Sdk **17.14.1**.
- Internet no primeiro restore. Todas as dependências NuGet estão fixadas nos `packages.lock.json`.

Os comandos abaixo partem da **raiz do repositório**, em PowerShell, bash ou terminal do IDE, salvo indicação. Instale o SDK pelo link acima e confira `dotnet --version`.

```sh
dotnet tool restore
dotnet restore AguaNaRua.sln --locked-mode
dotnet build AguaNaRua.sln --no-restore
dotnet test AguaNaRua.sln --no-build --no-restore
```

`dotnet tool restore` instala o EF local usando `dotnet-tools.json`; não precisa de ferramenta global.

## Banco e demonstração

O caminho relativo da conexão é resolvido a partir de `src/AguaNaRua.Api/`. O banco padrão será `src/AguaNaRua.Api/agua-na-rua.db`, ignorado pelo Git.

**Criar um banco vazio com schema**, sem dados de demonstração:

```sh
dotnet ef database update --project src/AguaNaRua.Api
```

**Preparar explicitamente a demonstração** (aplica migrations e encerra após o seed):

```sh
dotnet run --project src/AguaNaRua.Api --no-launch-profile -- --seed
```

O seed é transacional e idempotente: se já existe qualquer ponto, não altera dados nem renova horários. No primeiro seed, usa o relógio da inicialização: trecho A observado há 30 minutos, B há 48 horas e C sem relatos. Com a janela padrão de 24h, isso demonstra os três estados. Dados envelhecem normalmente nas próximas execuções.

Para uma demonstração nova sem remover o banco existente, aponte para **outro arquivo**. Exemplo PowerShell, com caminho relativo à pasta da API:

```powershell
$arquivoBanco = 'demonstracao-' + [Guid]::NewGuid().ToString('N') + '.db'
$env:ConnectionStrings__AguaNaRua = "Data Source=$arquivoBanco;Foreign Keys=True"
dotnet ef database update --project src/AguaNaRua.Api
dotnet run --project src/AguaNaRua.Api --no-launch-profile -- --seed
dotnet run --project src/AguaNaRua.Api --launch-profile http
# Ao terminar a sessão de demonstração:
# Remove-Item Env:ConnectionStrings__AguaNaRua
```

Use um nome de arquivo ainda inexistente para obter um banco realmente novo. Não é necessário remover o banco anterior. O schema tem FK obrigatória, exclusão restrita do ponto, propriedades obrigatórias, checks de coordenadas/enums/horários e índice `(PontoMonitoradoId, ObservadoEm, RegistradoEm, Id)`. A [migration inicial](src/AguaNaRua.Api/Infrastructure/Migrations/) e o snapshot estão incluídos.

## Executar a API

```sh
dotnet run --project src/AguaNaRua.Api --launch-profile http
```

- Base verificada: **http://localhost:5080**.
- Swagger UI verificado: **http://localhost:5080/swagger/index.html**.
- OpenAPI JSON verificado: **http://localhost:5080/swagger/v1/swagger.json**.
- A raiz `/` redireciona para o Swagger. Encerre com `Ctrl+C`.

O perfil `http` usa `Development`, aplica migrations e permite seed automaticamente. Fora de Development, as configurações padrão não aplicam migrations nem carregam seed: prepare o banco com os comandos anteriores. O comando explícito `--seed` é uma opção consciente em qualquer ambiente.

Configuração em `appsettings.json` e `appsettings.Development.json`, também substituível por variáveis de ambiente:

| Chave | Variável | Padrão |
|---|---|---|
| `ConnectionStrings:AguaNaRua` | `ConnectionStrings__AguaNaRua` | SQLite em arquivo local |
| `Atualizacao:JanelaHoras` | `Atualizacao__JanelaHoras` | 24; inteiro de 1 a 8760 |
| `Database:ApplyMigrations` | `Database__ApplyMigrations` | false; true em Development |
| `DemoSeed:Enabled` | `DemoSeed__Enabled` | false; true em Development |

Não há origem web acordada nos documentos; CORS não foi habilitado. O mobile nativo pode consumir a API. Para usar em dispositivo físico será preciso configurar endereço acessível ao dispositivo e `AllowedHosts` no ambiente. A configuração entregue escuta somente localhost para demonstração.

## Contrato REST

| Método | Caminho | Respostas |
|---|---|---|
| GET | `/api/pontos` | 200 com lista, inclusive vazia |
| GET | `/api/pontos/{id}` | 200, 400 para ID inválido, 404 |
| GET | `/api/pontos/{id}/observacoes?page=1&pageSize=20` | 200 com itens e metadados, 400, 404 |
| GET | `/api/pontos/{id}/observacoes/{observacaoId}` | 200, 400, 404 inclusive relato de outro ponto |
| POST | `/api/pontos/{id}/observacoes` | 201 + `Location`, 400, 404 |

IDs são inteiros positivos. `page`: 1 a 21474836 (limite que evita overflow do deslocamento); `pageSize`: 1 a 100, padrão 20. Página além do total retorna itens vazios. Ponto existente sem histórico retorna 200 com `itens: []`, `totalItens: 0` e `totalPaginas: 0`; ponto inexistente retorna 404.

POST aceita somente:

```json
{
  "condicao": "AGUA_ACUMULADA",
  "observadoEm": "2026-11-18T12:00:00Z"
}
```

Campos obrigatórios; valores de `condicao`: `SEM_AGUA_ACUMULADA_OBSERVADA`, `AGUA_ACUMULADA`, `ALAGAMENTO_RELATADO`. Valores numéricos, condições desconhecidas, campos adicionais (incluindo `origem`/`registradoEm`), corpo malformado e horário futuro retornam 400. Não há tolerância para relógio do cliente adiantado.

Envie ISO 8601 com segundos e `Z` ou offset explícito, por exemplo `2026-11-18T09:00:00-03:00`. A API normaliza e responde em UTC (`...Z`). O servidor preenche `registradoEm` com seu relógio e `origem` com `RELATO_CIDADAO`. Erros usam `ProblemDetails`/`ValidationProblemDetails`, `application/problem+json`, com `status`, `title` e, na validação, `errors`.

Os exemplos de data da documentação são estáticos; para registrar um relato recente em outra data, use o relógio atual ou o exemplo dinâmico abaixo.

## Leitura dos estados

| `statusAtualizacao` | Regra |
|---|---|
| `RECENTE` | Idade da última observação menor ou igual à janela configurada. O limite exato de 24h é recente. |
| `DESATUALIZADA` | Idade maior que a janela. Mantém condição, data e origem visíveis. |
| `SEM_OBSERVACAO` | Não há observação; `ultimaObservacao` é `null`. |

A idade usa **`observadoEm`**, nunca `registradoEm`. A última observação é a de maior `observadoEm`, com desempate por `registradoEm` e depois `id`, todos decrescentes. Um relato antigo registrado agora não substitui um relato observado depois. O histórico usa a mesma ordem.

No SQLite, horários são armazenados como **INTEGER de ticks UTC**, com conversão EF para `DateTimeOffset`. Isso permite ordenar/filtrar no banco e preservar precisão de ticks, evitando a limitação do provider para `DateTimeOffset` nativo. Os testes verificam offsets, desempate e o tipo real persistido.

`SEM_AGUA_ACUMULADA_OBSERVADA` é uma condição relatada em um instante, não uma declaração de segurança atual. Nenhum dos estados indica que o cidadão pode atravessar o trecho.

## Formas de reprodução para o professor

| Forma | O que demonstra | Ferramenta |
|---|---|---|
| A. Swagger no navegador | Contrato, schemas, respostas e execução de GET/POST | Navegador e API |
| B. Menu interativo no terminal | Consulta dos três estados, histórico paginado e registro guiado de observações | API no primeiro terminal e Console no segundo |
| C. Requisições HTTP | Requisições reproduzíveis, corpo JSON e consulta do relato criado | Arquivo `.http` ou PowerShell |

As três formas usam o **SDK .NET 10.0.200**, internet no primeiro restore e a mesma API local. Não é necessário instalar um servidor SQLite. Abra o terminal na raiz do repositório e execute esta preparação comum (comandos válidos em PowerShell ou bash):

```sh
dotnet tool restore
dotnet restore AguaNaRua.sln --locked-mode
dotnet build AguaNaRua.sln --no-restore
dotnet ef database update --project src/AguaNaRua.Api
dotnet run --project src/AguaNaRua.Api --no-launch-profile -- --seed
```

O banco padrão fica em `src/AguaNaRua.Api/agua-na-rua.db`. O seed só preenche um banco sem pontos. Para obter novamente os três estados com horários relativos ao dia da avaliação, siga [Banco e demonstração](#banco-e-demonstração) e escolha outro arquivo antes da preparação. Não é necessário apagar bancos existentes.

### A. Swagger no navegador

Após a preparação comum, inicie a API e mantenha esse terminal aberto:

```sh
dotnet run --project src/AguaNaRua.Api --launch-profile http
```

Abra **http://localhost:5080/swagger/index.html**. Em cada operação, clique em **Try it out** e depois **Execute**:

1. `GET /api/pontos`: retorna os pontos. No banco demonstrativo novo, aparecem `RECENTE`, `DESATUALIZADA` e `SEM_OBSERVACAO`.
2. `GET /api/pontos/{id}`, com `id=3`: retorna `ultimaObservacao=null`, indicando ausência de informação. Consulte também `id=1` para ver data, origem e condição.
3. Para `POST /api/pontos/{id}/observacoes`, com `id=1`, gere um corpo com horário recente no **PowerShell** e cole o JSON produzido no Swagger:

```powershell
@{
    condicao = 'AGUA_ACUMULADA'
    observadoEm = [DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('o')
} | ConvertTo-Json
```

O POST retorna **201** e `Location`; consulte o endereço retornado para obter o relato pelo seu ID. Para testar validação, envie `condicao: "INVALIDA"` e veja **400**. Consulte um ponto inexistente, como `999999`, para obter **404**. As datas dos exemplos visuais do Swagger são estáticas; o comando acima gera uma data recente para o dia da avaliação. Encerre a API com `Ctrl+C` no terminal.

### B. Menu interativo no terminal

Use os mesmos pré-requisitos e a preparação comum. No **primeiro terminal**, execute a API:

```sh
dotnet run --project src/AguaNaRua.Api --launch-profile http
```

No **segundo terminal**, também na raiz do repositório:

```sh
dotnet run --project src/AguaNaRua.Console
```

Os dois processos devem permanecer em execução durante o uso. O Console usa `HttpClient`, sem ler ou gravar diretamente o banco. O menu oferece:

```text
Água na Rua

1. Listar pontos monitorados
2. Consultar detalhes de um ponto
3. Consultar histórico de observações
4. Registrar uma observação
0. Sair
```

A opção 1 mostra identificação, bairro, indicação de demonstração, estado e última observação. A opção 2 inclui coordenadas; experimente o ponto 3 para visualizar ausência de observação. Na opção 3, o histórico usa páginas de **5 relatos**: `N` avança, `A` volta e `0` retorna ao menu. Sem histórico, o Console informa ausência de informação e retorna ao menu. Cada operação concluída volta ao menu principal.

Na opção 4, escolha a condição em um menu numerado e informe **Agora (UTC)** ou um instante ISO 8601 com segundos e `Z`/offset explícito. A entrada manual mantém o instante informado; a transmissão e a exibição usam UTC, identificado por `Z (UTC)`. Um horário sem fuso é recusado pelo Console; um horário futuro é enviado à API, que devolve o erro de validação. `origem` e `registradoEm` sempre vêm do servidor.

Pontos fictícios são marcados como demonstração. Relatos do seed mostram `DEMONSTRACAO`; os enviados pelo Console usam o endpoint público e recebem `RELATO_CIDADAO`, mesmo em um ponto demonstrativo. Recência é o estado recebido da API, com janela padrão de 24h; o Console não recalcula essa regra nem interpreta ausência de registros como passagem livre.

O endereço padrão é `http://localhost:5080`. Para outra API, informe uma URL absoluta HTTP/HTTPS, sem credenciais, query ou fragmento:

```sh
dotnet run --project src/AguaNaRua.Console -- --api-url http://localhost:5082
```

O endereço informado deve corresponder à API em execução. Há timeout HTTP de **10 segundos**, sem repetição automática de requisições. Erros 400/404 e mensagens `ProblemDetails` são apresentados no terminal; falhas de conexão, timeout e respostas incompatíveis retornam ao menu. Digite `0` no menu principal para sair. `Ctrl+C` cancela e encerra o Console; fim da entrada padrão também encerra normalmente. Depois, use `Ctrl+C` no primeiro terminal para parar a API. Para ver os argumentos:

```sh
dotnet run --project src/AguaNaRua.Console -- --help
```

### C. Requisições HTTP

Após a preparação comum, mantenha a API em execução com `dotnet run --project src/AguaNaRua.Api --launch-profile http`. Abra [AguaNaRua.http](AguaNaRua.http) no VS Code com a extensão **REST Client**. Clique em **Send Request** acima do bloco desejado. Execute o bloco `criarObservacao` antes do GET que usa o ID retornado. A variável `{{$datetime iso8601 -1 m}}` gera o horário de um minuto atrás a cada execução. Para outra porta, ajuste `@baseUrl` no início do arquivo.

Também é possível executar este exemplo completo no **PowerShell**. Ele usa somente os dois campos aceitos e uma data dinâmica:

```powershell
$base = 'http://localhost:5080'
Invoke-RestMethod "$base/api/pontos"
Invoke-RestMethod "$base/api/pontos/3"
$body = @{
    condicao = 'AGUA_ACUMULADA'
    observadoEm = [DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('o')
} | ConvertTo-Json
$relato = Invoke-RestMethod "$base/api/pontos/1/observacoes" -Method Post -ContentType 'application/json' -Body $body
Invoke-RestMethod "$base/api/pontos/1/observacoes/$($relato.id)"
Invoke-RestMethod "$base/api/pontos/1/observacoes?page=1&pageSize=20"
```

O POST feito por `Invoke-RestMethod` retorna o corpo criado em `$relato`. A requisição seguinte consulta o mesmo ID. O arquivo `.http` e o Swagger permitem inspecionar também os status HTTP e o cabeçalho `Location`. O exemplo pressupõe os IDs do seed em um banco novo.

## Resolver problemas de execução

| Situação | Como resolver |
|---|---|
| Porta 5080 ocupada | Use a API que já está aberta ou encerre-a com `Ctrl+C` no terminal correspondente. Outra opção é iniciar em outra porta, como no comando abaixo. |
| Console informa API indisponível | Confira o endereço exibido e inicie a API no primeiro terminal. Se mudou a porta, passe a mesma URL em `--api-url`. O Console continua no menu para nova operação. |
| Banco sem dados / lista vazia | Pare a API e execute `dotnet run --project src/AguaNaRua.Api --no-launch-profile -- --seed` com a mesma conexão. Se já existem pontos, o seed não modifica o banco: use outro arquivo para uma demonstração completa. |
| Dados ficaram antigos | O seed não renova relatos existentes. Prepare outro arquivo conforme [Banco e demonstração](#banco-e-demonstração). |
| Banco sem schema / execução sem perfil | Aplique `dotnet ef database update --project src/AguaNaRua.Api` na mesma conexão. O perfil `http` já aplica migrations em Development. |
| URL inválida, timeout ou JSON incompatível | Use o endereço base desta Web API, não o endereço do Swagger. O Console valida a URL, informa falhas de conexão ou de contrato e não repete automaticamente um POST. |
| Build com MSB3021/MSB3027 no Windows | Um executável da configuração em compilação pode estar em uso. Encerre API/Console com `Ctrl+C` nos respectivos terminais antes de recompilar. A [auditoria documental](docs/revisao-documental.md) verificou Release sem encerrar a API que já estava usando Debug. |

Exemplo de API em outra porta (**PowerShell ou bash**), no primeiro terminal:

```sh
dotnet run --project src/AguaNaRua.Api --launch-profile http -- --urls http://localhost:5082
```

No segundo terminal, use o comando do Console da [forma B](#b-menu-interativo-no-terminal) com `--api-url http://localhost:5082`. O Swagger estará em `http://localhost:5082/swagger/index.html`. Na [forma C](#c-requisições-http), altere `@baseUrl` do arquivo `.http` ou `$base` do exemplo PowerShell para essa URL.

Para desfazer uma conexão definida por variável no **PowerShell**, pare a API e execute `Remove-Item Env:ConnectionStrings__AguaNaRua` no terminal em que ela foi definida. Isso remove a variável da sessão, sem apagar o arquivo SQLite.

## Organização e rubrica de C#

Uma solution, um projeto Web API, um projeto Console e um projeto xUnit. A separação por pastas mantém as responsabilidades explícitas. O Console não referencia o projeto da API; os DTOs do consumidor seguem o contrato HTTP existente. O workflow já executa build e testes da solution inteira, incluindo o novo projeto, sem precisar de alteração:

| Avaliação | Evidência no código |
|---|---|
| POO — 3,0 | [Domain](src/AguaNaRua.Api/Domain/): entidades encapsuladas, fábricas com invariantes, navegação somente de leitura e enums fechados. `TimeProvider` permite polimorfismo nos testes. |
| Web API — 2,5 | [PontosController](src/AguaNaRua.Api/Api/PontosController.cs): cinco operações REST, DTOs separados, validação, ProblemDetails, 201/Location e Swagger com exemplos. |
| EF Core + LINQ — 1,5 | [AguaDbContext](src/AguaNaRua.Api/Infrastructure/AguaDbContext.cs), migrations e [PontoRepository](src/AguaNaRua.Api/Infrastructure/PontoRepository.cs): projeção, subconsulta da última observação, ordenação, paginação e `AsNoTracking`. A lista faz duas consultas, sem N+1. |
| Async/Await — 1,0 | Controller → serviço → repositório: `CancellationToken`, `AnyAsync`, `SingleOrDefaultAsync`, `CountAsync`, `ToListAsync`, `ToDictionaryAsync`, `SaveChangesAsync`. |
| Qualidade — 2,0 | [Application](src/AguaNaRua.Api/Application/): interfaces específicas, DI, política de recência testável; [testes](tests/AguaNaRua.Tests/), configuração, versões fixadas e evidências reproduzíveis. |

`Api/` trata HTTP e serialização; `Application/` coordena os casos de uso e DTOs; `Domain/` protege invariantes; `Infrastructure/` implementa a persistência. Controladores não recebem nem retornam entidades EF ou `IQueryable`.

## Verificações e evidências

Na implementação inicial, em **18/11/2026**, passaram **49 testes** de domínio, serviço e integração da API. Os [registros anteriores](docs/evidencias/README.md) foram preservados. Na alteração do Console, em **19/11/2026**, passaram **83 testes**: os 49 existentes e 34 casos adicionais de configuração, contratos HTTP, erros, timestamps, menu, paginação, encerramento e integração Console → API → SQLite. Restore e build também passaram, com zero avisos e erros. O workflow de CI já compila e testa a solution inteira, incluindo o Console, e não precisou de alteração.

Para repetir build e todos os testes (**PowerShell ou bash**):

```sh
dotnet restore AguaNaRua.sln --locked-mode
dotnet build AguaNaRua.sln --no-restore
dotnet test AguaNaRua.sln --no-build --no-restore
```

Na auditoria documental de **19/11/2026**, o restore foi reconferido. O build Debug encontrou o executável da API bloqueado por um processo já em execução. Preservando esse processo, o build **Release** terminou com zero avisos e erros e os **83 testes passaram** nessa configuração, conforme o [registro da revisão](docs/revisao-documental.md). Os comandos adicionais verificados, válidos em **PowerShell ou bash**, foram:

```sh
dotnet build AguaNaRua.sln --no-restore --configuration Release
dotnet test AguaNaRua.sln --no-build --no-restore --configuration Release --logger 'console;verbosity=minimal'
```

As [evidências da inclusão do Console](docs/evidencias/console/README.md) incluem transcrições reais dos processos, entradas usadas, API com banco isolado, POST seguido de consulta, paginação, erros e API desligada. Para repetir essa verificação no **Windows PowerShell**, sem regravar os arquivos da execução anterior da API:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verificar-Console.ps1
```

O script usa porta **5082**, banco novo em `tmp/` e grava em `docs/evidencias/console/`. Aceita `-Porta 5083` quando necessário e encerra somente os processos que ele próprio iniciou. Os relatos gerados são fictícios de demonstração. As entradas do menu são fornecidas pela entrada padrão aos processos reais do Console; o script não depende de inspeção visual do terminal.

A verificação anterior da API também pode ser repetida no Windows (porta 5081 e banco isolado; **regrava** os arquivos originais em `docs/evidencias/`):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verificar-Entrega.ps1
```

O script executa restore/build/testes, verifica divergência do modelo, aplica a migration em banco novo, sobe a API, consulta OpenAPI, faz GET/POST e reinicia o processo para verificar persistência. Regrava as evidências da execução atual. Use `-Porta 5082` se necessário. Não interfere no banco padrão da demonstração.

- [Registro das evidências](docs/evidencias/README.md), com comandos, configuração, status e trechos reais.
- [Build](docs/evidencias/build.txt), [testes](docs/evidencias/testes.txt), [restore](docs/evidencias/restore.txt), [migration em banco novo](docs/evidencias/migrations.txt), [modelo sem alterações pendentes](docs/evidencias/modelo.txt).
- [Requisições verificadas](docs/evidencias/http.json) e [OpenAPI capturado](docs/evidencias/openapi.json).
- [Conferência das URLs da demonstração em localhost:5080](docs/evidencias/demonstracao-local.json).
- [Consulta histórica de vulnerabilidades das dependências](docs/evidencias/dependencias.txt), da execução de 18/11/2026: não apontou pacotes vulneráveis nas fontes consultadas naquela execução.
- Captura gráfica indisponível: a ferramenta de controle do computador bloqueou o Firefox por falta de suporte à política de URLs nesse navegador no Windows. Swagger UI verificado por HTTP 200; as evidências são respostas reais, sem screenshot.
- [Workflow de CI](.github/workflows/dotnet.yml) configurado para GitHub Actions em pushes e pull requests. Consulte as execuções na [aba Actions](https://github.com/vbastelli/GS-2-CS/actions).

## Estado da entrega e pendências

| Item | Situação documentada |
|---|---|
| Implementação técnica de C# | Web API, Console HTTP, EF Core/SQLite, migrations e testes próprios presentes na solution. Execuções comprovadas nas evidências acima. |
| Build e testes | 49 testes na etapa inicial de 18/11/2026; 83 após inclusão do Console em 19/11/2026, com build sem avisos ou erros. As duas etapas possuem registros separados. |
| Repositório GitHub | [vbastelli/GS-2-CS](https://github.com/vbastelli/GS-2-CS) reúne a implementação e a documentação da entrega. A publicação no GitHub e o envio acadêmico são etapas distintas; o recebimento no Teams/Portal deve ser confirmado pelo integrante responsável. |
| Pesquisa com moradores e pontos piloto | Pendente: não foram fornecidos registros de entrevistas, validação da necessidade atual ou pontos reais verificados. Os três pontos da API continuam fictícios. |
| Indicadores de impacto | Pendente: não foram fornecidas medições de tempo de consulta, compreensão da informação ou impacto em campo. |
| Contrato com o mobile | Pendente de confirmação pela equipe; o contrato desta API está no Swagger. Favoritos pertencem à persistência local do mobile. |
| Complementos da turma/professor | Devem ser conferidos pela equipe; não foram fornecidos além dos documentos existentes. |
| Envio no Teams/Portal do Aluno | Sem confirmação ou comprovante fornecido. O integrante responsável deve enviar o link do GitHub e conferir o recebimento na tarefa da disciplina. |

O [registro desta auditoria documental](docs/revisao-documental.md) relaciona as verificações e suas fontes.

Conforme a página 30 e as regras gerais do [edital fornecido](docs/GS%20CitizenTech%20-%203ES%20Fev%20-%20FIAP%202026.pdf), C# é enviado como **link do GitHub**, por **um integrante** no Teams ou Portal do Aluno, até **19/11/2026 às 23h55**, conforme orientação da turma. Não há vídeo específico de C# entre os entregáveis da página 30. O envio acadêmico fica a cargo do integrante responsável.
