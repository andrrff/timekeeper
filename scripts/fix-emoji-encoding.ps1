# Script para corrigir problemas de codificação de emojis no Windows

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Correção de Codificação UTF-8" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test current console settings
Write-Host "Testando configurações atuais do console..." -ForegroundColor Yellow
Write-Host "Code Page atual: $([Console]::OutputEncoding.CodePage)" -ForegroundColor Gray
Write-Host "Encoding atual: $([Console]::OutputEncoding.EncodingName)" -ForegroundColor Gray
Write-Host ""

# Test emoji display
Write-Host "Testando exibição de emojis:" -ForegroundColor Yellow
$testEmojis = @("📋", "⏱️", "📊", "⚙️", "🔄", "❌", "🚀")
foreach ($emoji in $testEmojis) {
    Write-Host "  $emoji - $(if ($emoji.Length -eq 2) { 'OK' } else { 'PROBLEMA' })" -ForegroundColor $(if ($emoji.Length -eq 2) { 'Green' } else { 'Red' })
}
Write-Host ""

# Check if we need to fix encoding
$needsFix = [Console]::OutputEncoding.CodePage -ne 65001

if ($needsFix) {
    Write-Host "Problema detectado! Aplicando correções..." -ForegroundColor Yellow
    Write-Host ""
    
    # Method 1: Set UTF-8 encoding
    Write-Host "1. Configurando UTF-8 no console atual..." -ForegroundColor Cyan
    try {
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        [Console]::InputEncoding = [System.Text.Encoding]::UTF8
        Write-Host "   ✅ UTF-8 configurado com sucesso" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Falha ao configurar UTF-8" -ForegroundColor Red
    }
    
    # Method 2: Registry fix for persistent UTF-8
    Write-Host "2. Configurando UTF-8 permanente no registro..." -ForegroundColor Cyan
    try {
        $regPath = "HKLM:\SYSTEM\CurrentControlSet\Control\Nls\CodePage"
        Set-ItemProperty -Path $regPath -Name "ACP" -Value "65001" -Force
        Set-ItemProperty -Path $regPath -Name "OEMCP" -Value "65001" -Force
        Write-Host "   ✅ Registro atualizado (requer reinicialização)" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Falha ao atualizar registro (pode precisar de admin)" -ForegroundColor Red
    }
    
    # Method 3: Create a UTF-8 launcher
    Write-Host "3. Criando launcher UTF-8..." -ForegroundColor Cyan
    $tkUtf8Path = Join-Path $env:LOCALAPPDATA "tk-utf8.bat"
    $tkUtf8Content = @"
@echo off
chcp 65001 >nul 2>&1
set PYTHONIOENCODING=utf-8
tk %*
"@
    Set-Content -Path $tkUtf8Path -Value $tkUtf8Content
    Write-Host "   ✅ Launcher criado: $tkUtf8Path" -ForegroundColor Green
    Write-Host "      Use 'tk-utf8' em vez de 'tk' se houver problemas" -ForegroundColor Gray
    
} else {
    Write-Host "✅ Codificação UTF-8 já está configurada corretamente!" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "             Soluções Manuais" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Se os emojis ainda não aparecerem corretamente, tente:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Terminal Windows/PowerShell:" -ForegroundColor Cyan
Write-Host "   - Vá em Configurações > Perfis > Padrões" -ForegroundColor White
Write-Host "   - Em 'Adicional' > 'Codificação de texto' > selecione 'UTF-8'" -ForegroundColor White
Write-Host ""
Write-Host "2. Prompt de Comando:" -ForegroundColor Cyan
Write-Host "   - Execute: chcp 65001" -ForegroundColor White
Write-Host "   - Depois execute: tk" -ForegroundColor White
Write-Host ""
Write-Host "3. PowerShell:" -ForegroundColor Cyan
Write-Host "   - Execute: `$OutputEncoding = [console]::InputEncoding = [console]::OutputEncoding = New-Object System.Text.UTF8Encoding" -ForegroundColor White
Write-Host "   - Depois execute: tk" -ForegroundColor White
Write-Host ""
Write-Host "4. Fonte do Terminal:" -ForegroundColor Cyan
Write-Host "   - Use uma fonte que suporte emojis como:" -ForegroundColor White
Write-Host "     • Cascadia Code" -ForegroundColor Gray
Write-Host "     • Consolas" -ForegroundColor Gray
Write-Host "     • Segoe UI Emoji" -ForegroundColor Gray
Write-Host ""

# Test again after fixes
Write-Host "Testando novamente após correções:" -ForegroundColor Yellow
foreach ($emoji in $testEmojis) {
    Write-Host "  $emoji - $(if ($emoji.Length -eq 2) { 'OK' } else { 'AINDA COM PROBLEMA' })" -ForegroundColor $(if ($emoji.Length -eq 2) { 'Green' } else { 'Red' })
}

Write-Host ""
Write-Host "Pressione qualquer tecla para continuar..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
