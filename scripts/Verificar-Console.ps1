param([ValidateRange(1024, 65535)][int]$Porta = 5082)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$pasta = Join-Path $repo 'docs/evidencias/console'
$temporarios = Join-Path $repo ('tmp/console-' + [Guid]::NewGuid().ToString('N'))
$banco = Join-Path $temporarios 'console.db'
$baseUrl = "http://localhost:$Porta"
$processoApi = $null
$registros = [System.Collections.Generic.List[object]]::new()
$nomesEnv = @('ASPNETCORE_ENVIRONMENT', 'ConnectionStrings__AguaNaRua', 'Database__ApplyMigrations', 'DemoSeed__Enabled')
$ambienteAnterior = @{}
New-Item -ItemType Directory -Force -Path $pasta, $temporarios | Out-Null

function Salvar([string]$Arquivo, [string]$Texto) {
    $limpo = $Texto.Replace($repo, '<repositorio>').Replace($env:USERPROFILE, '<perfil>')
    if ($Arquivo.EndsWith('.txt') -and -not $Arquivo.EndsWith('.entrada.txt')) {
        # Mantém o conteúdo da saída; remove apenas espaços finais de apresentação.
        $limpo = [regex]::Replace($limpo, '(?m)[ \t]+(?=\r?$)', '').TrimEnd("`r", "`n") + "`n"
    }
    [IO.File]::WriteAllText((Join-Path $pasta $Arquivo), $limpo, [Text.UTF8Encoding]::new($false))
}

function Dotnet-Verificado([string[]]$Argumentos, [string]$Arquivo) {
    $linhas = & dotnet @Argumentos 2>&1
    $codigo = $LASTEXITCODE
    Salvar $Arquivo (("dotnet " + ($Argumentos -join ' ')) + "`n" + (($linhas | ForEach-Object { $_.ToString() }) -join "`n") + "`nExitCode: $codigo`n")
    if ($codigo -ne 0) { throw "Falhou: dotnet $($Argumentos -join ' ')" }
    Write-Output "OK: $Arquivo"
}

function Console-Verificado([string]$Nome, [string[]]$Entradas, [string[]]$Esperados,
    [string]$Argumentos = "--api-url $baseUrl", [int]$ExitCode = 0) {
    $config = [Diagnostics.ProcessStartInfo]::new()
    $config.FileName = 'dotnet'
    $config.Arguments = "run --project src/AguaNaRua.Console --no-build -- $Argumentos"
    $config.WorkingDirectory = $repo
    $config.UseShellExecute = $false
    $config.CreateNoWindow = $true
    $config.RedirectStandardInput = $true
    $config.RedirectStandardOutput = $true
    $config.RedirectStandardError = $true
    $config.StandardOutputEncoding = [Text.Encoding]::UTF8
    $config.StandardErrorEncoding = [Text.Encoding]::UTF8
    $processo = [Diagnostics.Process]::new()
    $processo.StartInfo = $config
    try {
        $null = $processo.Start()
        $saidaTask = $processo.StandardOutput.ReadToEndAsync()
        $erroTask = $processo.StandardError.ReadToEndAsync()
        foreach ($linha in $Entradas) { $processo.StandardInput.WriteLine($linha) }
        $processo.StandardInput.Close()
        if (-not $processo.WaitForExit(30000)) {
            & taskkill.exe /PID $processo.Id /T /F | Out-Null
            throw "O Console não terminou no cenário $Nome."
        }
        $saida = $saidaTask.GetAwaiter().GetResult()
        $erro = $erroTask.GetAwaiter().GetResult()
        Salvar "$Nome.txt" ("dotnet $($config.Arguments)`n" + $saida + $erro + "`nExitCode: $($processo.ExitCode)`n")
        Salvar "$Nome.entrada.txt" (($Entradas -join "`n") + "`n")
        if ($processo.ExitCode -ne $ExitCode) { throw "Exit code inesperado em $Nome." }
        foreach ($esperado in $Esperados) {
            if (-not ($saida + $erro).Contains($esperado)) { throw "Cenário $Nome sem o resultado esperado: $esperado" }
        }
        $registros.Add([ordered]@{cenario=$Nome; exitCode=$processo.ExitCode; verificacoes=$Esperados; saida="$Nome.txt"; entrada="$Nome.entrada.txt"})
        Write-Output "OK: $Nome"
    } finally { $processo.Dispose() }
}

function Parar-ApiConsole {
    if ($null -ne $script:processoApi -and -not $script:processoApi.HasExited) {
        & taskkill.exe /PID $script:processoApi.Id /T /F | Out-Null
        $null = $script:processoApi.WaitForExit(10000)
    }
}

Push-Location $repo
try {
    foreach ($nome in $nomesEnv) { $ambienteAnterior[$nome] = [Environment]::GetEnvironmentVariable($nome, 'Process') }
    if (Get-NetTCPConnection -LocalPort $Porta -State Listen -ErrorAction SilentlyContinue) {
        throw "Porta $Porta ocupada. Use -Porta com outra porta livre."
    }
    Dotnet-Verificado @('tool', 'restore') 'ferramentas.txt'
    Dotnet-Verificado @('restore', 'AguaNaRua.sln', '--locked-mode') 'restore.txt'
    Dotnet-Verificado @('build', 'AguaNaRua.sln', '--no-restore') 'build.txt'
    Dotnet-Verificado @('test', 'AguaNaRua.sln', '--no-build', '--no-restore', '--logger', 'console;verbosity=minimal') 'testes.txt'
    Dotnet-Verificado @('ef', 'database', 'update', '--project', 'src/AguaNaRua.Api', '--no-build', '--connection', "Data Source=$banco;Foreign Keys=True") 'migrations.txt'

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__AguaNaRua = "Data Source=$banco;Foreign Keys=True"
    $env:Database__ApplyMigrations = 'true'
    $env:DemoSeed__Enabled = 'true'
    $processoApi = Start-Process -FilePath 'dotnet' -ArgumentList @(
        'run', '--project', 'src/AguaNaRua.Api', '--no-build', '--no-launch-profile', '--urls', $baseUrl
    ) -WorkingDirectory $repo -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $temporarios 'api.log') -RedirectStandardError (Join-Path $temporarios 'api-erros.log')
    $pronta = $false
    for ($tentativa = 0; $tentativa -lt 60; $tentativa++) {
        if ($processoApi.HasExited) { throw 'A API encerrou durante a inicialização.' }
        try { $pontos = Invoke-RestMethod "$baseUrl/api/pontos" -TimeoutSec 2; $pronta = $true; break } catch { }
        Start-Sleep -Milliseconds 250
    }
    if (-not $pronta) { throw 'A API não ficou disponível.' }
    if (($pontos.statusAtualizacao -join ',') -ne 'RECENTE,DESATUALIZADA,SEM_OBSERVACAO') { throw 'Seed sem os três estados esperados.' }
    Salvar 'pontos-iniciais.json' (ConvertTo-Json -InputObject $pontos -Depth 10)

    $futuro = [DateTimeOffset]::UtcNow.AddDays(1).ToString('o')
    Console-Verificado '01-menu-completo' @(
        '1','2','1','2','3','3','3','3','1','0','4','1','2','1','2','1','3','1','0',
        '9','','2','abc','','999999','4','1','9','2','9','2',$futuro,'0'
    ) @('ID: 1','ID: 2','ID: 3','RECENTE','DESATUALIZADA','SEM_OBSERVACAO','Coordenadas:',
        'Sem observações: ausência de informação','HTTP 201','RELATO_CIDADAO','Opção inválida','ID inválido',
        'HTTP 404','Condição inválida','Opção de horário inválida','HTTP 400','O horário observado não pode estar no futuro.','Até logo!')
    $ponto = Invoke-RestMethod "$baseUrl/api/pontos/1"
    if ($ponto.ultimaObservacao.origem -ne 'RELATO_CIDADAO') { throw 'O POST do Console não persistiu.' }
    $relato = Invoke-RestMethod "$baseUrl/api/pontos/1/observacoes/$($ponto.ultimaObservacao.id)"
    Salvar 'relato-criado-e-consultado.json' ($relato | ConvertTo-Json -Depth 8)

    $offset = [DateTimeOffset]::UtcNow.AddMinutes(-1).ToOffset([TimeSpan]::FromHours(-3)).ToString('o')
    Console-Verificado '02-horario-com-offset' @('4','1','1','2','sem-fuso','2',$offset,'3','1','0','0') @(
        'Horário inválido','HTTP 201','SEM_AGUA_ACUMULADA_OBSERVADA','(UTC)','Histórico do ponto 1','Até logo!')
    $historicoOffset = Invoke-RestMethod "$baseUrl/api/pontos/1/observacoes"
    $relatoOffset = @($historicoOffset.itens | Where-Object { $_.condicao -eq 'SEM_AGUA_ACUMULADA_OBSERVADA' })
    if ($relatoOffset.Count -ne 1 -or ([DateTimeOffset]$relatoOffset[0].observadoEm).UtcDateTime.Ticks -ne ([DateTimeOffset]$offset).UtcDateTime.Ticks) {
        throw 'O instante informado com offset não foi preservado.'
    }
    Salvar 'relato-com-offset.json' ($relatoOffset[0] | ConvertTo-Json)
    # Completa duas páginas com relatos fictícios no banco isolado.
    for ($i = 0; $i -lt 3; $i++) {
        $corpo = @{condicao='AGUA_ACUMULADA'; observadoEm=[DateTimeOffset]::UtcNow.AddMinutes(-2 - $i).ToString('o')} | ConvertTo-Json
        $null = Invoke-RestMethod "$baseUrl/api/pontos/1/observacoes" -Method Post -ContentType 'application/json' -Body $corpo
    }
    Console-Verificado '03-paginacao' @('3','1','n','a','n','n','0','0') @('página 1 de 2','página 2 de 2','Navegação inválida','Até logo!')
    Console-Verificado '04-fim-da-entrada' @('2') @('Entrada encerrada')
    Console-Verificado '05-url-invalida' @() @('A URL da API deve ser absoluta') '--api-url ftp://localhost' 2
    Console-Verificado '06-ajuda' @() @('Timeout HTTP: 10 segundos') '--help'

    # Executa o exemplo HTTP dinâmico do README e confere o relato pelo endpoint de detalhe.
    $corpoHttp = @{condicao='AGUA_ACUMULADA'; observadoEm=[DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('o')} | ConvertTo-Json
    $criadoHttp = Invoke-RestMethod "$baseUrl/api/pontos/1/observacoes" -Method Post -ContentType 'application/json' -Body $corpoHttp
    $consultadoHttp = Invoke-RestMethod "$baseUrl/api/pontos/1/observacoes/$($criadoHttp.id)"
    if ($consultadoHttp.id -ne $criadoHttp.id) { throw 'O exemplo HTTP do README falhou.' }
    Salvar 'exemplo-http-readme.json' ($consultadoHttp | ConvertTo-Json)
    $swagger = Invoke-WebRequest "$baseUrl/swagger/index.html" -UseBasicParsing
    $openapi = Invoke-WebRequest "$baseUrl/swagger/v1/swagger.json" -UseBasicParsing
    if ($swagger.StatusCode -ne 200 -or $openapi.StatusCode -ne 200) { throw 'Documentação HTTP indisponível.' }
    Parar-ApiConsole
    Console-Verificado '07-api-desligada' @('1','0') @("Não foi possível acessar a API em $baseUrl",'dotnet run --project src/AguaNaRua.Api --launch-profile http','Até logo!')
    [ordered]@{
        executadoEmUtc=[DateTimeOffset]::UtcNow.ToString('o'); sdk=(& dotnet --version)
        apiUrl=$baseUrl; banco='SQLite novo e isolado em tmp/ (ignorado pelo Git)'
        demonstracao='Três pontos fictícios; seed DEMONSTRACAO; novos relatos enviados pelo endpoint público RELATO_CIDADAO'
        swaggerStatus=200; openapiStatus=200; persistenciaConferida=$true
        cenarios=$registros
    } | ConvertTo-Json -Depth 12 | ForEach-Object { Salvar 'verificacao.json' $_ }
    Write-Output 'Verificação do Console concluída. Evidências anteriores preservadas; novas em docs/evidencias/console/.'
} finally {
    Parar-ApiConsole
    foreach ($nome in $nomesEnv) { [Environment]::SetEnvironmentVariable($nome, $ambienteAnterior[$nome], 'Process') }
    Pop-Location
}
