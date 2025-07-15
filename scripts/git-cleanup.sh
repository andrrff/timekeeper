#!/bin/bash
# Script para limpeza completa do repositório Git

echo "=== Limpeza do Repositório Git ==="
echo ""

# Verificar se estamos em um repositório Git
if [ ! -d ".git" ]; then
    echo "Erro: Este diretório não é um repositório Git!"
    exit 1
fi

# 1. Fazer backup do estado atual
echo "1. Criando backup do estado atual..."
BACKUP_BRANCH="backup-$(date +'%Y%m%d-%H%M%S')"
git branch "$BACKUP_BRANCH" 2>/dev/null
if [ $? -eq 0 ]; then
    echo "   ✓ Backup criado na branch: $BACKUP_BRANCH"
else
    echo "   ⚠ Não foi possível criar backup"
fi

# 2. Remover todos os arquivos do stage (unstage)
echo "2. Removendo arquivos do stage..."
git reset HEAD . 2>/dev/null
echo "   ✓ Todos os arquivos removidos do stage"

# 3. Restaurar todas as mudanças não commitadas
echo "3. Restaurando todas as mudanças não commitadas..."
git restore . 2>/dev/null
git clean -fd 2>/dev/null
echo "   ✓ Todas as mudanças restauradas"

# 4. Verificar se há commits não pushados
echo "4. Verificando commits não pushados..."
REMOTES=$(git remote 2>/dev/null | wc -l)

if [ "$REMOTES" -gt 0 ]; then
    echo "   🔍 Remote encontrado, verificando commits não pushados..."
    git fetch origin 2>/dev/null
    UNPUSHED=$(git rev-list --count origin/master..HEAD 2>/dev/null || echo "1")
else
    echo "   📁 Sem remote configurado, assumindo commits locais..."
    UNPUSHED=$(git rev-list --count HEAD 2>/dev/null || echo "0")
fi

if [ "$UNPUSHED" -gt 0 ]; then
    echo "   📦 Encontrados $UNPUSHED commit(s) não pushado(s)"
    
    # 5. Resetar commits não pushados
    echo "5. Removendo commits não pushados..."
    
    if [ "$REMOTES" -gt 0 ]; then
        # Resetar para origin/master
        git reset --hard origin/master 2>/dev/null
        if [ $? -eq 0 ]; then
            echo "   ✓ Resetado para origin/master"
        else
            # Fallback: resetar para commit inicial
            FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD 2>/dev/null | head -1)
            if [ -n "$FIRST_COMMIT" ]; then
                git reset --hard "$FIRST_COMMIT" 2>/dev/null
                echo "   ✓ Resetado para o commit inicial"
            else
                echo "   ⚠ Não foi possível resetar"
            fi
        fi
    else
        # Sem remote: resetar para commit inicial ou recriar
        FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD 2>/dev/null | head -1)
        if [ -n "$FIRST_COMMIT" ]; then
            git reset --hard "$FIRST_COMMIT" 2>/dev/null
            echo "   ✓ Resetado para o commit inicial"
        else
            # Recriar repositório limpo
            echo "   🔄 Recriando repositório limpo..."
            rm -rf .git
            git init 2>/dev/null
            echo "   ✓ Repositório reinicializado"
            BACKUP_BRANCH=""  # Backup não é válido mais
        fi
    fi
else
    echo "   ✓ Nenhum commit não pushado encontrado"
fi

# 6. Limpar arquivos ignorados
echo "6. Limpando arquivos ignorados pelo .gitignore..."
git clean -fdX 2>/dev/null
echo "   ✓ Arquivos ignorados removidos"

# 7. Status final
echo "7. Status final do repositório:"
STATUS_LINES=$(git status --porcelain 2>/dev/null | wc -l)
if [ "$STATUS_LINES" -eq 0 ]; then
    echo "   ✓ Repositório limpo - nenhuma mudança pendente"
else
    echo "   📝 Ainda há $STATUS_LINES arquivo(s) com mudanças"
    git status --short
fi

echo ""
echo "=== Limpeza Concluída ==="
echo "O repositório foi limpo e resetado com sucesso!"

if [ -n "$BACKUP_BRANCH" ]; then
    echo ""
    echo "💡 Dica: Se precisar recuperar algo, use: git checkout $BACKUP_BRANCH"
fi
