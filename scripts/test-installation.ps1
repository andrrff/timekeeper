# Exemplo de uso da variável de ambiente TK

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Testando a instalação do Timekeeper" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar se o comando tk está disponível
Write-Host "Testando se o comando 'tk' está disponível..." -ForegroundColor Yellow

try {
    $tkPath = (Get-Command "tk" -ErrorAction Stop).Source
    Write-Host "✅ Comando 'tk' encontrado: $tkPath" -ForegroundColor Green
} catch {
    Write-Host "❌ Comando 'tk' não encontrado no PATH!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possíveis soluções:" -ForegroundColor Yellow
    Write-Host "1. Reinicie o terminal/prompt de comando" -ForegroundColor White
    Write-Host "2. Execute a instalação novamente" -ForegroundColor White
    Write-Host "3. Verifique se a instalação foi bem-sucedida" -ForegroundColor White
    exit 1
}

# Verificar se o executável existe
if (Test-Path $tkPath) {
    Write-Host "✅ Executável encontrado e acessível" -ForegroundColor Green
} else {
    Write-Host "❌ Executável não encontrado no caminho especificado" -ForegroundColor Red
    exit 1
}

# Testar execução do comando
Write-Host ""
Write-Host "Testando execução do comando..." -ForegroundColor Yellow
Write-Host ""

# Testar execução do comando
Write-Host ""
Write-Host "Testando execução do comando..." -ForegroundColor Yellow
Write-Host ""

# Test emoji support first
Write-Host "Verificando suporte a emojis..." -ForegroundColor Yellow
$testEmojis = @("📋", "⏱️", "📊", "⚙️", "🔄", "❌")
$emojiIssues = 0

foreach ($emoji in $testEmojis) {
    if ($emoji.Length -ne 2) {
        $emojiIssues++
    }
}

if ($emojiIssues -gt 0) {
    Write-Host "⚠️  Problema de codificação detectado!" -ForegroundColor Yellow
    Write-Host "   $emojiIssues emojis não estão sendo exibidos corretamente" -ForegroundColor Red
    Write-Host "   Execute: .\scripts\fix-emoji-encoding.ps1 para corrigir" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "✅ Emojis sendo exibidos corretamente!" -ForegroundColor Green
    Write-Host ""
}

try {
    Write-Host "Executando: tk --help" -ForegroundColor Gray
    & tk --help
    Write-Host ""
    Write-Host "✅ Comando executado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "❌ Erro ao executar o comando 'tk'" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "           Teste concluído!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Agora você pode usar 'tk' de qualquer lugar!" -ForegroundColor Green
Write-Host ""
Write-Host "Exemplos de uso:" -ForegroundColor Yellow
Write-Host "  tk                 # Abrir interface principal" -ForegroundColor White
Write-Host "  tk --help          # Mostrar ajuda" -ForegroundColor White
Write-Host "  tk --version       # Mostrar versão" -ForegroundColor White
