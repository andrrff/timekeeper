# Timekeeper CLI - Installers & Database Tools

Este diretório contém vários scripts de instalação e ferramentas de banco de dados para o Timekeeper CLI, cada um adequado para diferentes cenários de uso.

## 🚀 Instalação Rápida

### Instalação Automática (Recomendada)
```powershell
# Execute no PowerShell
.\scripts\install.ps1
```
Este script detecta automaticamente se você tem privilégios de administrador e escolhe o método de instalação apropriado. **Agora inclui migração automática do banco de dados!**

## 📋 Opções de Instalação

### 1. Instalação do Sistema (Requer Administrador)
```powershell
# PowerShell como Administrador
.\scripts\install-windows.ps1
```
```batch
# Prompt de Comando como Administrador
.\scripts\install-windows.bat
```
**Características:**
- Instala em `C:\Program Files\Timekeeper`
- Adiciona à variável PATH do sistema
- Disponível para todos os usuários
- Cria entradas no menu Iniciar
- Registra no "Adicionar ou Remover Programas"

### 2. Instalação do Usuário (Sem Administrador)
```powershell
.\scripts\install-user.ps1
```
**Características:**
- Instala em `%LOCALAPPDATA%\Timekeeper`
- Adiciona à variável PATH do usuário
- Disponível apenas para o usuário atual
- Não requer privilégios de administrador

### 3. Instalação de Desenvolvimento
```powershell
.\scripts\install-dev.ps1
```
**Características:**
- Aponta para a saída de build em tempo real
- Ideal para desenvolvimento e testes
- Comando `tk-dev` para versão explícita
- Script de rebuild incluído

## 🏗️ Build e Distribuição

### Build para Múltiplas Plataformas
```powershell
# Build padrão (Windows x64)
.\scripts\build.ps1

# Build com múltiplas plataformas
.\scripts\build.ps1 -RuntimeIdentifiers @("win-x64", "win-x86", "linux-x64") -CreateZip

# Build self-contained
.\scripts\build.ps1 -SelfContained -CreateZip
```

### Criar Instalador MSI (Requer WiX Toolset)
```powershell
.\scripts\create-msi-installer.ps1
```

## 🎯 Comando `tk`

Após a instalação, você pode usar o comando `tk` de qualquer lugar:

```bash
# Executar Timekeeper
tk

# Exemplos de uso (depende da implementação do seu CLI)
tk add "Nova tarefa"
tk list
tk start 1
tk stop
tk report
```

## 🔧 Scripts Incluídos

| Script | Descrição | Privilégios |
|--------|-----------|-------------|
| `install.ps1` | Instalação automática | Detecta automaticamente |
| `install-windows.ps1` | Instalação do sistema (PowerShell) | Administrador |
| `install-windows.bat` | Instalação do sistema (Batch) | Administrador |
| `install-user.ps1` | Instalação do usuário | Usuário normal |
| `install-dev.ps1` | Instalação de desenvolvimento | Usuário normal |
| `build.ps1` | Build e empacotamento | Usuário normal |
| `create-msi-installer.ps1` | Criar instalador MSI | Usuário normal |

## 💾 Gerenciamento de Banco de Dados

### Scripts de Banco de Dados

| Script | Descrição | Uso |
|--------|-----------|-----|
| `migrate-database.ps1` | Aplica migrações e otimiza o banco | `.\migrate-database.ps1` |
| `db-status.ps1` | Verifica status do banco e migrações | `.\db-status.ps1 -Detailed` |
| `convert-to-migrations.ps1` | Converte banco EnsureCreated() para migrações | `.\convert-to-migrations.ps1 -Force` |

### Migração Automática do Banco de Dados
Todos os scripts de instalação agora incluem migração automática do banco de dados:

```powershell
# Verificar status do banco
.\scripts\db-status.ps1

# Aplicar migrações manualmente
.\scripts\migrate-database.ps1

# Migração com opções avançadas
.\scripts\migrate-database.ps1 -Verbose -Force

# Converter banco antigo para usar migrações
.\scripts\convert-to-migrations.ps1 -Force
```

### Recursos de Migração
- ✅ **Migração Automática**: Todos os instaladores executam migrações automaticamente
- ✅ **Backup Automático**: Backups automáticos antes de mudanças importantes
- ✅ **Otimização**: Comando VACUUM e ANALYZE para melhor performance
- ✅ **Verificação de Integridade**: Verifica estado do banco antes das operações
- ✅ **Conversão de Banco**: Converte bancos criados com EnsureCreated()

### Solução de Problemas

#### Erro "table already exists"
Se você receber o erro de tabela já existente:

```powershell
# Opção 1: Converter banco existente
.\scripts\convert-to-migrations.ps1 -Force

# Opção 2: Forçar recriação com backup
.\scripts\migrate-database.ps1 -Force
```

#### Verificar Status
```powershell
# Status detalhado com schema
.\scripts\db-status.ps1 -Detailed -ShowSchema
```

## 🗂️ Estrutura de Instalação

### Instalação do Sistema
```
C:\Program Files\Timekeeper\
├── Timekeeper.CLI.exe
├── tk.bat
├── TimeKeeper.ico
├── README.md
├── uninstall.bat (ou uninstall.ps1)
└── [outras dependências]
```

### Instalação do Usuário
```
%LOCALAPPDATA%\Timekeeper\
├── Timekeeper.CLI.exe
├── tk.bat
├── tk.ps1
├── TimeKeeper.ico
├── README.md
├── uninstall.ps1
└── [outras dependências]
```

### Instalação de Desenvolvimento
```
%LOCALAPPDATA%\Timekeeper-Dev\
├── tk.bat (aponta para build output)
├── tk-dev.bat
├── rebuild.ps1
└── uninstall-dev.ps1
```

## 🚨 Resolução de Problemas

### Comando `tk` não encontrado
1. **Reinicie o prompt de comando** - mudanças no PATH requerem novo terminal
2. **Verifique o PATH** - execute `echo $env:PATH` (PowerShell) ou `echo %PATH%` (CMD)
3. **Execute como administrador** - se usando instalação do sistema

### Erro de build
1. **Verifique o .NET** - execute `dotnet --version`
2. **Instale o .NET 9.0** - https://dotnet.microsoft.com/
3. **Verifique as dependências** - execute `dotnet restore`

### Problemas de permissão
1. **Use install-user.ps1** - não requer administrador
2. **Execute PowerShell como administrador** - para instalação do sistema
3. **Verifique política de execução** - execute `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### Emojis aparecendo como "??" ou "□"
Este é um problema comum de codificação UTF-8 no Windows. Soluções:

1. **Execute o corretor automático**:
   ```powershell
   .\scripts\fix-emoji-encoding.ps1
   ```

2. **Configuração manual do terminal**:
   ```cmd
   chcp 65001
   tk
   ```

3. **PowerShell**:
   ```powershell
   $OutputEncoding = [console]::InputEncoding = [console]::OutputEncoding = New-Object System.Text.UTF8Encoding
   tk
   ```

4. **Terminal Windows**: Vá em Configurações → Perfis → Padrões → Adicional → selecione "UTF-8"

5. **Use fonte compatível**: Cascadia Code, Consolas, ou Segoe UI Emoji

## 📝 Desinstalação

### Instalação do Sistema
- Use "Adicionar ou Remover Programas" no Windows
- Ou execute o uninstaller: `C:\Program Files\Timekeeper\uninstall.bat`

### Instalação do Usuário
```powershell
& "$env:LOCALAPPDATA\Timekeeper\uninstall.ps1"
```

### Instalação de Desenvolvimento
```powershell
& "$env:LOCALAPPDATA\Timekeeper-Dev\uninstall-dev.ps1"
```

## 🔗 Variável de Ambiente

Todos os instaladores configuram a variável de ambiente PATH para incluir o diretório do Timekeeper, permitindo que você use o comando `tk` de qualquer lugar no sistema.

- **Sistema**: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment`
- **Usuário**: `HKCU\Environment`

## 📄 Licença

Os scripts de instalação seguem a mesma licença do projeto Timekeeper.
