# Evidências reais da API de C#

Execução: **2026-11-18T18:13:28.6343144+00:00 (UTC)**. Ambiente Windows, SDK **10.0.200**, runtime .NET **10.0.4**, EF Core **10.0.4**, provider **Microsoft.EntityFrameworkCore.Sqlite**. Banco em arquivo novo e isolado em `tmp/`, ignorado pelo Git; nenhum banco externo foi usado.

Comando executado na raiz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verificar-Entrega.ps1
```

Configuração: `Development`, URL `http://localhost:5081`, `Database:ApplyMigrations=true`, `DemoSeed:Enabled=true`, `Atualizacao:JanelaHoras=24`. Seed relativo à primeira inicialização: três referências fictícias no Jardim Peri (`demonstracao=true`), duas observações com origem `DEMONSTRACAO` e um ponto sem observação. Não são pontos validados ou medições oficiais.

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
| GET | `/api/pontos` | 200 | [JSON](respostas/01.json) |
| GET | `/api/pontos/3` | 200 | [JSON](respostas/02.json) |
| GET | `/api/pontos/3/observacoes?page=1&pageSize=20` | 200 | [JSON](respostas/03.json) |
| POST | `/api/pontos/1/observacoes` | 201 | [JSON](respostas/04.json) |
| GET | `/api/pontos/1/observacoes/3` | 200 | [JSON](respostas/05.json) |
| GET | `/api/pontos/1/observacoes?page=1&pageSize=1` | 200 | [JSON](respostas/06.json) |
| POST | `/api/pontos/1/observacoes` | 400 | [JSON](respostas/07.json) |
| POST | `/api/pontos/1/observacoes` | 400 | [JSON](respostas/08.json) |
| GET | `/api/pontos/999999` | 404 | [JSON](respostas/09.json) |
| GET | `/api/pontos/2/observacoes/3` | 404 | [JSON](respostas/10.json) |
| GET | `/api/pontos/1/observacoes/3` | 200 | [JSON](respostas/11.json) |

### Listagem com os três estados

Campos selecionados da resposta real de `GET /api/pontos`; [resposta completa](respostas/01.json):

```json
[
    {
        "id":  1,
        "demonstracao":  true,
        "statusAtualizacao":  "RECENTE",
        "observadoEm":  "2026-11-18T17:43:24.9560633Z",
        "origem":  "DEMONSTRACAO"
    },
    {
        "id":  2,
        "demonstracao":  true,
        "statusAtualizacao":  "DESATUALIZADA",
        "observadoEm":  "2026-11-16T18:13:24.9560633Z",
        "origem":  "DEMONSTRACAO"
    },
    {
        "id":  3,
        "demonstracao":  true,
        "statusAtualizacao":  "SEM_OBSERVACAO",
        "observadoEm":  null,
        "origem":  null
    }
]
```

### Ponto sem observação

`GET /api/pontos/3` retornou **200**, com `ultimaObservacao=null`:

```json
{"id":3,"nome":"Trecho C — demonstração fictícia","bairro":"Jardim Peri","latitude":-23.447,"longitude":-46.659,"demonstracao":true,"statusAtualizacao":"SEM_OBSERVACAO","ultimaObservacao":null}
```

O [histórico desse ponto](respostas/03.json) retornou **200**, `itens=[]` e totais iguais a zero.

### Criação seguida de consulta

Corpo enviado a `POST /api/pontos/1/observacoes`:

```json
{
    "condicao":  "SEM_AGUA_ACUMULADA_OBSERVADA",
    "observadoEm":  "2026-11-18T18:12:26.0088639+00:00"
}
```

Retorno **201**, `Location: http://localhost:5081/api/pontos/1/observacoes/3`:

```json
{"id":3,"pontoId":1,"condicao":"SEM_AGUA_ACUMULADA_OBSERVADA","origem":"RELATO_CIDADAO","observadoEm":"2026-11-18T18:12:26.0088639Z","registradoEm":"2026-11-18T18:13:26.0635231Z"}
```

A consulta de `Location` retornou **200** e o mesmo relato: [GET após POST](respostas/05.json). O script encerrou e iniciou novamente o processo da API, usando o mesmo arquivo SQLite, e repetiu o GET: **200**, com o mesmo ID e horário observado, em [GET após reinício](respostas/11.json). O seed não duplicou os dados.

### Validação e ausência de recurso

Condição desconhecida: **400** em [resposta de validação](respostas/07.json). Horário futuro: **400**, com o corpo:

```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Dados inválidos.","status":400,"instance":"/api/pontos/1/observacoes","errors":{"observadoEm":["O horário observado não pode estar no futuro."]},"traceId":"00-c696bd7db2fc49db3f5bb3b039f6cc5a-ec2115b2e043c5d5-00"}
```

Ponto inexistente: **404** em [resposta de recurso ausente](respostas/09.json). Relato associado a outro ponto: **404** em [resposta de associação incorreta](respostas/10.json). Os erros usam `application/problem+json`.

## Limitação da captura gráfica

Nesta entrega a ferramenta de controle do computador interrompeu a captura porque a política de URLs não é suportada no Firefox disponível neste Windows. Não foi produzida captura gráfica. O Swagger foi verificado por HTTP 200 e seu OpenAPI por teste automatizado e requisição real. As evidências acima não dependem de imagens de interface.
