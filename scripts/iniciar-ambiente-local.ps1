# Sobe o ambiente de teste local: API, Worker e frontend.
# Usa a conexao do arquivo .env da raiz deste repositorio, sem gravar nada no Windows.
#
# Uso:
#   .\scripts\iniciar-ambiente-local.ps1                 (sobe API, Worker e frontend)
#   .\scripts\iniciar-ambiente-local.ps1 -SemWorker      (sobe API e frontend, sem Worker)
#   .\scripts\iniciar-ambiente-local.ps1 -SemFrontend    (sobe API e Worker, sem frontend)

param(
    [switch]$SemWorker,
    [switch]$SemFrontend
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $raiz '.env'
$frontendDir = Join-Path $raiz 'frontend'
$apiUrl = 'http://localhost:5077'

if (-not (Test-Path $envFile)) {
    throw "Arquivo .env nao encontrado em $raiz"
}

# Le as variaveis do .env sem exibir os valores
$vars = @{}
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([A-Z_]+)=(.*)$') { $vars[$matches[1]] = $matches[2] }
}

foreach ($chave in 'POSTGRES_HOST', 'POSTGRES_PORT', 'POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD') {
    if ([string]::IsNullOrWhiteSpace($vars[$chave])) {
        throw "Variavel $chave ausente no .env"
    }
}

# Variaveis herdadas pelos processos iniciados abaixo (somente nesta sessao)
$env:ConnectionStrings__YallaFinance = "Host=$($vars['POSTGRES_HOST']);Port=$($vars['POSTGRES_PORT']);Database=$($vars['POSTGRES_DB']);Username=$($vars['POSTGRES_USER']);Password=$($vars['POSTGRES_PASSWORD'])"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'

# 1. API
$apiProj = Join-Path $raiz 'src\YallaFinance.WebApi'
Write-Host "Subindo a API em $apiUrl ..."
Start-Process powershell -ArgumentList '-NoExit', '-Command', "dotnet run --project '$apiProj' --no-launch-profile --urls $apiUrl" -WorkingDirectory $raiz

# 2. Worker
if (-not $SemWorker) {
    Write-Warning 'O Worker processa Jobs e sincroniza com os provedores habilitados no banco (Nibo, Sicoob, Asaas).'
    $workerProj = Join-Path $raiz 'src\YallaFinance.Worker'
    Start-Process powershell -ArgumentList '-NoExit', '-Command', "dotnet run --project '$workerProj'" -WorkingDirectory $raiz
    Write-Host 'Worker iniciado em uma janela separada.'
}
else {
    Write-Host 'Worker nao iniciado (-SemWorker).'
}

# 3. Frontend
$frontendUrl = $null
if (-not $SemFrontend) {
    if (-not (Test-Path (Join-Path $frontendDir 'node_modules'))) {
        Write-Host 'Instalando dependencias do frontend (npm ci) ...'
        Push-Location $frontendDir
        try { npm ci } finally { Pop-Location }
    }

    $envLocal = Join-Path $frontendDir '.env.local'
    if (-not (Test-Path $envLocal)) {
        Copy-Item (Join-Path $frontendDir '.env.example') $envLocal
    }
    # Garante API_URL apontando para a API local
    $conteudo = Get-Content $envLocal | Where-Object { $_ -notmatch '^\s*API_URL=' }
    Set-Content -Path $envLocal -Value (@("API_URL=$apiUrl") + $conteudo)

    # Porta 3000 se livre; senao 3001 (nao encerra o processo que ja esta usando 3000)
    $porta = 3000
    if (Get-NetTCPConnection -LocalPort 3000 -State Listen -ErrorAction SilentlyContinue) { $porta = 3001 }
    $frontendUrl = "http://localhost:$porta"
    Start-Process powershell -ArgumentList '-NoExit', '-Command', "npm run dev -- -p $porta" -WorkingDirectory $frontendDir
    Write-Host "Frontend iniciado em $frontendUrl (janela separada)."
}
else {
    Write-Host 'Frontend nao iniciado (-SemFrontend).'
}

# 4. Verificacao da API
Write-Host 'Aguardando a API responder em /health ...'
$saudavel = $false
for ($i = 0; $i -lt 60 -and -not $saudavel; $i++) {
    Start-Sleep -Seconds 2
    try {
        $resposta = Invoke-RestMethod "$apiUrl/health" -TimeoutSec 3
        $saudavel = $true
        Write-Host "API respondeu: $resposta"
    }
    catch { }
}
if (-not $saudavel) {
    Write-Warning 'A API nao respondeu em 120 segundos. Confira a janela da API para ver o erro.'
}

Write-Host ''
Write-Host "API:      $apiUrl"
if ($frontendUrl) { Write-Host "Frontend: $frontendUrl/login" }
Write-Host 'Para encerrar, feche as janelas abertas pelo script.'
