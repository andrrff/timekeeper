# Script simples para limpeza do repositório Git
param(
    [switch]$Force
)

Write-Host "=== Limpeza do Repositório Git ===" -ForegroundColor Yellow
Write-Host ""

# Verificar se estamos em um repositório Git
if (-not (Test-Path ".git")) {
    Write-Host "Erro: Este diretório não é um repositório Git!" -ForegroundColor Red
    exit 1
}

# 1. Remover todos os arquivos do stage
Write-Host "1. Removendo arquivos do stage..." -ForegroundColor Cyan
git reset HEAD . | Out-Null
Write-Host "   ✓ Arquivos removidos do stage" -ForegroundColor Green

# 2. Restaurar mudanças
Write-Host "2. Restaurando mudanças..." -ForegroundColor Cyan
git restore . | Out-Null
git clean -fd | Out-Null
Write-Host "   ✓ Mudanças restauradas" -ForegroundColor Green

# 3. Verificar commits não pushados
Write-Host "3. Verificando commits..." -ForegroundColor Cyan
$commits = git log --oneline | Measure-Object -Line | Select-Object -ExpandProperty Lines
Write-Host "   📦 Encontrados $commits commit(s) locais" -ForegroundColor Yellow

# 4. Resetar para commit inicial se houver mais de 1 commit
if ($commits -gt 1 -or $Force) {
    Write-Host "4. Resetando commits..." -ForegroundColor Cyan
    $firstCommit = git rev-list --max-parents=0 HEAD
    if ($firstCommit) {
        git reset --hard $firstCommit | Out-Null
        Write-Host "   ✓ Resetado para o primeiro commit" -ForegroundColor Green
    }
}

# 5. Limpar arquivos ignorados
Write-Host "5. Limpando arquivos ignorados..." -ForegroundColor Cyan
git clean -fdX | Out-Null
Write-Host "   ✓ Arquivos ignorados removidos" -ForegroundColor Green

# 6. Status final
Write-Host "6. Status final:" -ForegroundColor Cyan
$status = git status --porcelain
if ($status) {
    Write-Host "   📝 Mudanças restantes:" -ForegroundColor Yellow
    git status --short
} else {
    Write-Host "   ✓ Repositório limpo" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Limpeza Concluída ===" -ForegroundColor Green
