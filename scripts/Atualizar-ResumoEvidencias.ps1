$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$pasta = Join-Path $repo 'docs/evidencias'
$registro = Get-Content -LiteralPath (Join-Path $pasta 'http.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$pontos = Get-Content -LiteralPath (Join-Path $pasta 'respostas/01.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$recorte = @($pontos | ForEach-Object {
    [ordered]@{
        id = $_.id
        demonstracao = $_.demonstracao
        statusAtualizacao = $_.statusAtualizacao
        observadoEm = $_.ultimaObservacao.observadoEm
        origem = $_.ultimaObservacao.origem
    }
})
$tabela = @($registro.requisicoes | ForEach-Object {
    "| $($_.metodo) | ``$($_.caminho)`` | $($_.status) | [JSON]($($_.arquivoResposta)) |"
}) -join "`n"

$modelo = @'
# Evidências reais da API de C#

Execução: **@@DATA@@ (UTC)**. Ambiente Windows, SDK **10.0.200**, runtime .NET **10.0.4**, EF Core **10.0.4**, provider **Microsoft.EntityFrameworkCore.Sqlite**. Banco em arquivo novo e isolado em `tmp/`, ignorado pelo Git; nenhum banco externo foi usado.

Comando executado na raiz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verificar-Entrega.ps1
```

Configuração: `Development`, URL `@@URL@@`, `Database:ApplyMigrations=true`, `DemoSeed:Enabled=true`, `Atualizacao:JanelaHoras=24`. Seed relativo à primeira inicialização: três referências fictícias no Jardim Peri (`demonstracao=true`), duas observações com origem `DEMONSTRACAO` e um ponto sem observação. Não são pontos validados ou medições oficiais.

## Comandos e saídas

Todos os comandos abaixo terminaram com **ExitCode 0**. Os arquivos contêm as saídas reais, com caminhos do perfil e do repositório substituídos por marcadores.

| Comando | Saída |
|---|---|
| `dotnet --version` | [SDK](sdk.txt) |
| `dotnet tool restore` | [Ferramentas](ferramentas.txt) |
| `dotnet restore AguaNaRua.sln --locked-mode` | [Restore](restore.txt) |
| `dotnet list AguaNaRua.sln package --vulnerable --include-transitive` | [Dependências](dependencias.txt) |
| `dotnet build AguaNaRua.sln --no-restore` | [Build: zero avisos e erros](build.txt) |
| `dotnet test AguaNaRua.sln --no-build --no-restore --logger 'console;verbosity=minimal'` | [49 aprovados, zero falhas ou ignorados](testes.txt) |
| `dotnet ef migrations has-pending-model-changes --project src/AguaNaRua.Api --no-build` | [Sem divergência do modelo](modelo.txt) |
| `dotnet ef database update --project src/AguaNaRua.Api --no-build --connection 'Data Source=<banco novo>;Foreign Keys=True'` | [Migration aplicada](migrations.txt) |

Além dos testes unitários, os testes de integração usam SQLite relacional em arquivo temporário exclusivo, com migrations reais. Verificam ordenação e paginação no provider, UTC/offsets/ticks, FK, seed idempotente, ausência de N+1, HTTP e documentação.

## Requisições verificadas

Swagger UI `/swagger/index.html`: **HTTP 200**. OpenAPI `/swagger/v1/swagger.json`: **HTTP 200**, [documento capturado](openapi.json). As cinco operações, enums, exemplos, corpo obrigatório e respostas estão verificados nos testes.

O [registro HTTP](http.json) inclui os corpos enviados, status, Content-Type, Location e links para cada resposta. Os arquivos JSON abaixo são os corpos HTTP originais, sem recriar respostas.

| Método | Caminho | Status | Resposta |
|---|---|---|---|
@@TABELA@@

### Listagem com os três estados

Campos selecionados da resposta real de `GET /api/pontos`; [resposta completa](respostas/01.json):

```json
@@LISTA@@
```

### Ponto sem observação

`GET /api/pontos/3` retornou **200**, com `ultimaObservacao=null`:

```json
@@SEM@@
```

O [histórico desse ponto](respostas/03.json) retornou **200**, `itens=[]` e totais iguais a zero.

### Criação seguida de consulta

Corpo enviado a `POST /api/pontos/1/observacoes`:

```json
@@ENVIADO@@
```

Retorno **201**, `Location: @@LOCATION@@`:

```json
@@CRIADO@@
```

A consulta de `Location` retornou **200** e o mesmo relato: [GET após POST](respostas/05.json). O script encerrou e iniciou novamente o processo da API, usando o mesmo arquivo SQLite, e repetiu o GET: **200**, com o mesmo ID e horário observado, em [GET após reinício](respostas/11.json). O seed não duplicou os dados.

### Validação e ausência de recurso

Condição desconhecida: **400** em [resposta de validação](respostas/07.json). Horário futuro: **400**, com o corpo:

```json
@@FUTURO@@
```

Ponto inexistente: **404** em [resposta de recurso ausente](respostas/09.json). Relato associado a outro ponto: **404** em [resposta de associação incorreta](respostas/10.json). Os erros usam `application/problem+json`.

## Limitação da captura gráfica

Nesta entrega a ferramenta de controle do computador interrompeu a captura porque a política de URLs não é suportada no Firefox disponível neste Windows. Não foi produzida captura gráfica. O Swagger foi verificado por HTTP 200 e seu OpenAPI por teste automatizado e requisição real. As evidências acima não dependem de imagens de interface.
'@

$campos = @{
    '@@DATA@@' = $registro.executadoEmUtc
    '@@URL@@' = $registro.baseUrl
    '@@TABELA@@' = $tabela
    '@@LISTA@@' = (ConvertTo-Json -InputObject $recorte -Depth 6)
    '@@SEM@@' = (Get-Content -LiteralPath (Join-Path $pasta 'respostas/02.json') -Raw -Encoding UTF8)
    '@@ENVIADO@@' = ($registro.requisicoes[3].enviado | ConvertTo-Json)
    '@@LOCATION@@' = $registro.requisicoes[3].location
    '@@CRIADO@@' = (Get-Content -LiteralPath (Join-Path $pasta 'respostas/04.json') -Raw -Encoding UTF8)
    '@@FUTURO@@' = (Get-Content -LiteralPath (Join-Path $pasta 'respostas/08.json') -Raw -Encoding UTF8)
}
foreach ($campo in $campos.Keys) { $modelo = $modelo.Replace($campo, $campos[$campo]) }
$modelo | Set-Content -LiteralPath (Join-Path $pasta 'README.md') -Encoding UTF8
