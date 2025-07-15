# Script para limpeza completa do repositório Git
# Este script remove commits não pushados e faz restore de todos os arquivos em stage

Write-Host "=== Limpeza do Repositório Git ===" -ForegroundColor Yellow
Write-Host ""

# Verificar se estamos em um repositório Git
if (-not (Test-Path ".git")) {
    Write-Host "Erro: Este diretório não é um repositório Git!" -ForegroundColor Red
    exit 1
}

# 1. Fazer backup do estado atual (opcional)
Write-Host "1. Criando backup do estado atual..." -ForegroundColor Cyan
$backupBranch = "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
git branch $backupBranch 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Backup criado na branch: $backupBranch" -ForegroundColor Green
} else {
    Write-Host "   ⚠ Não foi possível criar backup" -ForegroundColor Yellow
}

# 2. Remover todos os arquivos do stage (unstage)
Write-Host "2. Removendo arquivos do stage..." -ForegroundColor Cyan
git reset HEAD . 2>$null
Write-Host "   ✓ Todos os arquivos removidos do stage" -ForegroundColor Green

# 3. Restaurar todas as mudanças não commitadas
Write-Host "3. Restaurando todas as mudanças não commitadas..." -ForegroundColor Cyan
git restore . 2>$null
git clean -fd 2>$null  # Remove arquivos não trackeados
Write-Host "   ✓ Todas as mudanças restauradas" -ForegroundColor Green

# 4. Verificar se há commits não pushados
Write-Host "4. Verificando commits não pushados..." -ForegroundColor Cyan
$hasRemote = git remote | Measure-Object -Line | Select-Object -ExpandProperty Lines

if ($hasRemote -gt 0) {
    $unpushedCommits = git rev-list --count HEAD..origin/master 2>$null
    if ($LASTEXITCODE -ne 0) {
        # Se não conseguir verificar, assumir que há commits locais
        $unpushedCommits = 1
    }
} else {
    # Sem remote configurado, assumir que todos os commits são locais
    $unpushedCommits = git rev-list --count HEAD 2>$null
    if ($LASTEXITCODE -ne 0) {
        $unpushedCommits = 0
    }
}

if ($unpushedCommits -gt 0) {
    Write-Host "   📦 Encontrados $unpushedCommits commit(s) não pushado(s)" -ForegroundColor Yellow
    
    # 5. Resetar para o último commit pushado ou commit inicial
    Write-Host "5. Removendo commits não pushados..." -ForegroundColor Cyan
    
    if ($hasRemote -gt 0) {
        # Tentar resetar para origin/master
        git fetch origin 2>$null
        if ($LASTEXITCODE -eq 0) {
            git reset --hard origin/master
            Write-Host "   ✓ Resetado para origin/master" -ForegroundColor Green
        } else {
            # Se falhar, resetar para o commit inicial
            $firstCommit = git rev-list --max-parents=0 HEAD 2>$null | Select-Object -First 1
            if ($firstCommit) {
                git reset --hard $firstCommit
                Write-Host "   ✓ Resetado para o commit inicial" -ForegroundColor Green
            } else {
                Write-Host "   ⚠ Não foi possível encontrar commit inicial" -ForegroundColor Yellow
            }
        }
    } else {
        # Sem remote, resetar para o commit inicial ou criar repositório limpo
        $firstCommit = git rev-list --max-parents=0 HEAD 2>$null | Select-Object -First 1
        if ($firstCommit) {
            git reset --hard $firstCommit
            Write-Host "   ✓ Resetado para o commit inicial" -ForegroundColor Green
        } else {
            # Recriar repositório limpo
            Write-Host "   🔄 Recriando repositório limpo..." -ForegroundColor Yellow
            Remove-Item -Recurse -Force .git
            git init
            Write-Host "   ✓ Repositório reinicializado" -ForegroundColor Green
        }
    }
} else {
    Write-Host "   ✓ Nenhum commit não pushado encontrado" -ForegroundColor Green
}

# 6. Limpar arquivos ignorados
Write-Host "6. Limpando arquivos ignorados pelo .gitignore..." -ForegroundColor Cyan
git clean -fdX 2>$null
Write-Host "   ✓ Arquivos ignorados removidos" -ForegroundColor Green

# 7. Status final
Write-Host "7. Status final do repositório:" -ForegroundColor Cyan
$statusLines = git status --porcelain | Measure-Object -Line | Select-Object -ExpandProperty Lines
if ($statusLines -eq 0) {
    Write-Host "   ✓ Repositório limpo - nenhuma mudança pendente" -ForegroundColor Green
} else {
    Write-Host "   📝 Ainda há $statusLines arquivo(s) com mudanças" -ForegroundColor Yellow
    git status --short
}

Write-Host ""
Write-Host "=== Limpeza Concluída ===" -ForegroundColor Green
Write-Host "O repositório foi limpo e resetado com sucesso!" -ForegroundColor Green

if ($backupBranch) {
    Write-Host ""
    Write-Host "💡 Dica: Se precisar recuperar algo, use: git checkout $backupBranch" -ForegroundColor Blue
}
