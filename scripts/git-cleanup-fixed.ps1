# Script para limpeza completa do repositório Git
# Este script remove commits não pushados e faz restore de todos os arquivos em stage

Write-Host "=== Limpeza do Repositório Git ===" -ForegroundColor Yellow
Write-Host ""

# Verificar se estamos em um repositório Git
if (-not (Test-Path ".git")) {
    Write-Host "Erro: Este diretório não é um repositório Git!" -ForegroundColor Red
    exit 1
}

try {
    # 1. Fazer backup do estado atual
    Write-Host "1. Criando backup do estado atual..." -ForegroundColor Cyan
    $backupBranch = "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    git branch $backupBranch 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ Backup criado na branch: $backupBranch" -ForegroundColor Green
    } else {
        Write-Host "   ⚠ Não foi possível criar backup" -ForegroundColor Yellow
    }

    # 2. Remover todos os arquivos do stage (unstage)
    Write-Host "2. Removendo arquivos do stage..." -ForegroundColor Cyan
    git reset HEAD . 2>&1 | Out-Null
    Write-Host "   ✓ Todos os arquivos removidos do stage" -ForegroundColor Green

    # 3. Restaurar todas as mudanças não commitadas
    Write-Host "3. Restaurando todas as mudanças não commitadas..." -ForegroundColor Cyan
    git restore . 2>&1 | Out-Null
    git clean -fd 2>&1 | Out-Null
    Write-Host "   ✓ Todas as mudanças restauradas" -ForegroundColor Green

    # 4. Verificar se há commits não pushados
    Write-Host "4. Verificando commits não pushados..." -ForegroundColor Cyan
    $remotes = git remote 2>&1
    $hasRemote = ($remotes -and $remotes.Count -gt 0 -and $remotes[0] -ne "")

    if ($hasRemote) {
        Write-Host "   🔍 Remote encontrado, verificando commits não pushados..." -ForegroundColor Yellow
        git fetch origin 2>&1 | Out-Null
        $unpushedCommits = git rev-list --count origin/master..HEAD 2>&1
        if ($LASTEXITCODE -ne 0) {
            $unpushedCommits = 1
        }
    } else {
        Write-Host "   📁 Sem remote configurado, assumindo commits locais..." -ForegroundColor Yellow
        $unpushedCommits = git rev-list --count HEAD 2>&1
        if ($LASTEXITCODE -ne 0) {
            $unpushedCommits = 0
        }
    }

    if ($unpushedCommits -gt 0) {
        Write-Host "   📦 Encontrados $unpushedCommits commit(s) não pushado(s)" -ForegroundColor Yellow
        
        # 5. Resetar commits não pushados
        Write-Host "5. Removendo commits não pushados..." -ForegroundColor Cyan
        
        if ($hasRemote) {
            # Resetar para origin/master
            git reset --hard origin/master 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✓ Resetado para origin/master" -ForegroundColor Green
            } else {
                # Fallback: resetar para commit inicial
                $firstCommit = git rev-list --max-parents=0 HEAD 2>&1 | Select-Object -First 1
                if ($firstCommit -and $firstCommit -match "^[a-f0-9]+$") {
                    git reset --hard $firstCommit 2>&1 | Out-Null
                    Write-Host "   ✓ Resetado para o commit inicial" -ForegroundColor Green
                } else {
                    Write-Host "   ⚠ Não foi possível resetar" -ForegroundColor Yellow
                }
            }
        } else {
            # Sem remote: resetar para commit inicial ou recriar
            $firstCommit = git rev-list --max-parents=0 HEAD 2>&1 | Select-Object -First 1
            if ($firstCommit -and $firstCommit -match "^[a-f0-9]+$") {
                git reset --hard $firstCommit 2>&1 | Out-Null
                Write-Host "   ✓ Resetado para o commit inicial" -ForegroundColor Green
            } else {
                # Recriar repositório limpo
                Write-Host "   🔄 Recriando repositório limpo..." -ForegroundColor Yellow
                Remove-Item -Recurse -Force .git -ErrorAction SilentlyContinue
                git init 2>&1 | Out-Null
                Write-Host "   ✓ Repositório reinicializado" -ForegroundColor Green
                $backupBranch = $null  # Backup não é válido mais
            }
        }
    } else {
        Write-Host "   ✓ Nenhum commit não pushado encontrado" -ForegroundColor Green
    }

    # 6. Limpar arquivos ignorados
    Write-Host "6. Limpando arquivos ignorados pelo .gitignore..." -ForegroundColor Cyan
    git clean -fdX 2>&1 | Out-Null
    Write-Host "   ✓ Arquivos ignorados removidos" -ForegroundColor Green

    # 7. Status final
    Write-Host "7. Status final do repositório:" -ForegroundColor Cyan
    $statusOutput = git status --porcelain 2>&1
    if ($statusOutput -and $statusOutput.Count -gt 0) {
        $statusLines = $statusOutput.Count
        Write-Host "   📝 Ainda há $statusLines arquivo(s) com mudanças" -ForegroundColor Yellow
        git status --short
    } else {
        Write-Host "   ✓ Repositório limpo - nenhuma mudança pendente" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "=== Limpeza Concluída ===" -ForegroundColor Green
    Write-Host "O repositório foi limpo e resetado com sucesso!" -ForegroundColor Green

    if ($backupBranch) {
        Write-Host ""
        Write-Host "💡 Dica: Se precisar recuperar algo, use: git checkout $backupBranch" -ForegroundColor Blue
    }

} catch {
    Write-Host ""
    Write-Host "❌ Erro durante a limpeza: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Verifique se o Git está instalado e funcionando corretamente." -ForegroundColor Yellow
}
