param(
    [ValidateRange(1024, 65535)]
    [int]$Porta = 5081
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$evidencias = Join-Path $repo 'docs/evidencias'
$execucao = Join-Path $repo ('tmp/verificacao-' + [Guid]::NewGuid().ToString('N'))
$banco = Join-Path $execucao 'verificacao.db'
$baseUrl = "http://localhost:$Porta"
$processoApi = $null
$http = $null
$variaveis = @('ASPNETCORE_ENVIRONMENT', 'ConnectionStrings__AguaNaRua', 'Database__ApplyMigrations', 'DemoSeed__Enabled')
$ambienteAnterior = @{}
$respostas = [System.Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Force -Path $evidencias, $execucao | Out-Null

function Ocultar-Caminhos([string]$Texto) {
    $Texto.Replace($repo, '<repositorio>').Replace($env:USERPROFILE, '<perfil>')
}

function Executar-Dotnet([string[]]$Argumentos, [string]$Arquivo) {
    $saida = & dotnet @Argumentos 2>&1
    $codigo = $LASTEXITCODE
    $texto = Ocultar-Caminhos (($saida | ForEach-Object { $_.ToString() }) -join "`n")
    @("dotnet $($Argumentos -join ' ')", $texto, "ExitCode: $codigo") |
        ForEach-Object { Ocultar-Caminhos $_ } |
        Set-Content -LiteralPath (Join-Path $evidencias $Arquivo) -Encoding UTF8
    if ($codigo -ne 0) { throw "Falha em dotnet $($Argumentos -join ' '): $codigo" }
    Write-Output (Ocultar-Caminhos "OK: dotnet $($Argumentos -join ' ')")
}

function Requisitar([string]$Metodo, [string]$Caminho, [int]$Esperado, $Corpo = $null) {
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Metodo), "$baseUrl$Caminho")
    try {
        if ($null -ne $Corpo) {
            $request.Content = [System.Net.Http.StringContent]::new(
                ($Corpo | ConvertTo-Json -Compress), [System.Text.Encoding]::UTF8, 'application/json')
        }
        $response = $http.SendAsync($request).GetAwaiter().GetResult()
        try {
            $texto = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $status = [int]$response.StatusCode
            if ($status -ne $Esperado) { throw "$Metodo $Caminho retornou $status (esperado $Esperado): $texto" }
            $dados = if ($texto) { $texto | ConvertFrom-Json } else { $null }
            $arquivoResposta = 'respostas/{0:D2}.json' -f ($respostas.Count + 1)
            [System.IO.File]::WriteAllText((Join-Path $evidencias $arquivoResposta), $texto,
                [System.Text.UTF8Encoding]::new($false))
            $respostas.Add([ordered]@{
                metodo = $Metodo; caminho = $Caminho; enviado = $Corpo; status = $status
                contentType = $response.Content.Headers.ContentType.ToString()
                location = [string]$response.Headers.Location; arquivoResposta = $arquivoResposta
            })
            Write-Output "HTTP $status $Metodo $Caminho" | Out-Host
            return $dados
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Parar-Api {
    if ($null -ne $script:processoApi -and -not $script:processoApi.HasExited) {
        # Encerra somente o processo iniciado por este script e seus filhos.
        & taskkill.exe /PID $script:processoApi.Id /T /F | Out-Null
        $script:processoApi.WaitForExit(10000) | Out-Null
    }
}

function Iniciar-Api {
    $script:processoApi = Start-Process -FilePath 'dotnet' -ArgumentList @(
        'run', '--project', 'src/AguaNaRua.Api', '--no-build', '--no-launch-profile', '--urls', $baseUrl
    ) -WorkingDirectory $repo -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $execucao 'api.log') `
        -RedirectStandardError (Join-Path $execucao 'api-erros.log')
    for ($tentativa = 0; $tentativa -lt 60; $tentativa++) {
        if ($script:processoApi.HasExited) { throw 'A API encerrou antes de iniciar. Consulte tmp/verificacao-*/api-erros.log.' }
        try {
            $response = $http.GetAsync("$baseUrl/swagger/v1/swagger.json").GetAwaiter().GetResult()
            $pronta = $response.IsSuccessStatusCode
            $response.Dispose()
            if ($pronta) { return }
        } catch { }
        Start-Sleep -Milliseconds 250
    }
    throw 'A API não iniciou dentro do prazo.'
}

Push-Location $repo
try {
    foreach ($nome in $variaveis) {
        $ambienteAnterior[$nome] = [Environment]::GetEnvironmentVariable($nome, 'Process')
    }
    Add-Type -AssemblyName System.Net.Http
    New-Item -ItemType Directory -Force -Path (Join-Path $evidencias 'respostas') | Out-Null
    $http = [System.Net.Http.HttpClient]::new()
    $http.Timeout = [TimeSpan]::FromSeconds(5)
    try {
        $ocupada = $http.GetAsync("$baseUrl/").GetAwaiter().GetResult()
        $ocupada.Dispose()
        throw "A porta $Porta já está em uso. Escolha outra com -Porta."
    } catch {
        if ($_.Exception.Message -like '*já está em uso*') { throw }
    }

    Executar-Dotnet @('--version') 'sdk.txt'
    Executar-Dotnet @('tool', 'restore') 'ferramentas.txt'
    Executar-Dotnet @('restore', 'AguaNaRua.sln', '--locked-mode') 'restore.txt'
    Executar-Dotnet @('list', 'AguaNaRua.sln', 'package', '--vulnerable', '--include-transitive') 'dependencias.txt'
    Executar-Dotnet @('build', 'AguaNaRua.sln', '--no-restore') 'build.txt'
    Executar-Dotnet @('test', 'AguaNaRua.sln', '--no-build', '--no-restore', '--logger', 'console;verbosity=minimal') 'testes.txt'
    Executar-Dotnet @('ef', 'migrations', 'has-pending-model-changes', '--project', 'src/AguaNaRua.Api', '--no-build') 'modelo.txt'
    if (Test-Path -LiteralPath $banco) { throw 'O banco desta verificação deveria estar vazio.' }
    Executar-Dotnet @('ef', 'database', 'update', '--project', 'src/AguaNaRua.Api', '--no-build',
        '--connection', "Data Source=$banco;Foreign Keys=True") 'migrations.txt'

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__AguaNaRua = "Data Source=$banco;Foreign Keys=True"
    $env:Database__ApplyMigrations = 'true'
    $env:DemoSeed__Enabled = 'true'
    Iniciar-Api

    $openapi = $http.GetStringAsync("$baseUrl/swagger/v1/swagger.json").GetAwaiter().GetResult()
    $openapi | Set-Content -LiteralPath (Join-Path $evidencias 'openapi.json') -Encoding UTF8
    $ui = $http.GetAsync("$baseUrl/swagger/index.html").GetAwaiter().GetResult()
    try {
        if ([int]$ui.StatusCode -ne 200) { throw 'Swagger UI não retornou 200.' }
    } finally { $ui.Dispose() }
    $pontos = Requisitar 'GET' '/api/pontos' 200
    if (($pontos.statusAtualizacao -join ',') -ne 'RECENTE,DESATUALIZADA,SEM_OBSERVACAO') {
        throw 'Os três estados iniciais não foram demonstrados.'
    }
    $sem = Requisitar 'GET' '/api/pontos/3' 200
    if ($null -ne $sem.ultimaObservacao) { throw 'O ponto 3 deveria estar sem observação.' }
    $null = Requisitar 'GET' '/api/pontos/3/observacoes?page=1&pageSize=20' 200
    $corpo = @{ condicao = 'SEM_AGUA_ACUMULADA_OBSERVADA'; observadoEm = [DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('o') }
    $criado = Requisitar 'POST' '/api/pontos/1/observacoes' 201 $corpo
    $location = $respostas[$respostas.Count - 1].location
    $path = if ([Uri]::IsWellFormedUriString($location, [UriKind]::Absolute)) { ([Uri]$location).PathAndQuery } else { $location }
    $consultado = Requisitar 'GET' $path 200
    if ($consultado.id -ne $criado.id -or $consultado.origem -ne 'RELATO_CIDADAO') { throw 'O relato criado não foi consultado corretamente.' }
    $null = Requisitar 'GET' '/api/pontos/1/observacoes?page=1&pageSize=1' 200
    $null = Requisitar 'POST' '/api/pontos/1/observacoes' 400 @{ condicao = 'INVALIDA'; observadoEm = $corpo.observadoEm }
    $null = Requisitar 'POST' '/api/pontos/1/observacoes' 400 @{ condicao = 'AGUA_ACUMULADA'; observadoEm = [DateTimeOffset]::UtcNow.AddDays(1).ToString('o') }
    $null = Requisitar 'GET' '/api/pontos/999999' 404
    $null = Requisitar 'GET' "/api/pontos/2/observacoes/$($criado.id)" 404

    Parar-Api
    Iniciar-Api
    $persistido = Requisitar 'GET' $path 200
    if ($persistido.id -ne $criado.id -or $persistido.observadoEm -ne $criado.observadoEm) {
        throw 'O relato não persistiu após reiniciar o processo.'
    }
    [ordered]@{
        executadoEmUtc = [DateTimeOffset]::UtcNow.ToString('o'); baseUrl = $baseUrl
        ambiente = 'Development'; banco = 'SQLite em arquivo novo e isolado em tmp/ (ignorado pelo Git)'
        janelaHoras = 24; seed = '3 pontos fictícios; 2 observações com origem DEMONSTRACAO'
        documentacao = @(
            @{ caminho = '/swagger/v1/swagger.json'; status = 200; arquivo = 'openapi.json' },
            @{ caminho = '/swagger/index.html'; status = 200 }
        )
        persistenciaAposReiniciar = $true; requisicoes = $respostas
    } | ConvertTo-Json -Depth 25 | Set-Content -LiteralPath (Join-Path $evidencias 'http.json') -Encoding UTF8
    & (Join-Path $PSScriptRoot 'Atualizar-ResumoEvidencias.ps1')
    Write-Output 'Verificação completa. Evidências em docs/evidencias/.'
} finally {
    Parar-Api
    if ($null -ne $http) { $http.Dispose() }
    foreach ($nome in $variaveis) {
        [Environment]::SetEnvironmentVariable($nome, $ambienteAnterior[$nome], 'Process')
    }
    Pop-Location
}
