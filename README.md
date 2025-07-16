```
  _____   _                      _
 |_   _| (_)  _ __ ___     ___  | | __   ___    ___   _ __     ___   _ __
   | |   | | | '_ ` _ \   / _ \ | |/ /  / _ \  / _ \ | '_ \   / _ \ | '__|
   | |   | | | | | | | | |  __/ |   <  |  __/ |  __/ | |_) | |  __/ | |
   |_|   |_| |_| |_| |_|  \___| |_|\_\  \___|  \___| | .__/   \___| |_|
                                                     |_|
```

# 🕐 Timekeeper - Advanced Time Management System

A comprehensive, cross-platform time management system with background monitoring, notifications, and DevOps integrations.

## ✨ Features

### 🚀 Core Features (Implemented)
- **Interactive CLI Interface** - Full-featured command-line interface for task management
- **Background Service** - Runs silently monitoring your tasks and timers
- **Cross-Platform** - Windows, Linux, and macOS support
- **Smart Notifications** - Get notified about overdue tasks, long-running timers, and external updates
- **System Tray Integration** - Quick access from your system tray (Windows)
- **DevOps Integration** - Sync with Azure DevOps and GitHub
- **Automated Installers** - MSI and portable packages for Windows with multi-architecture support

### ⏱️ Time Tracking (Implemented)
- **Automatic Timers** - Start/stop timers with automatic time calculation
- **Manual Time Entry** - Add time manually for past work
- **Multiple Active Timers** - Track time on multiple tasks simultaneously
- **Time Reports** - Daily, weekly, and custom time reports
- **Break Reminders** - Smart suggestions for breaks during long work sessions
- **Productivity Analytics** - Time distribution analysis and completion rates

### 📋 Task Management (Implemented)
- **Complete CRUD Operations** - Create, edit, delete, and manage tasks
- **Priority Levels** - Set and manage task priorities
- **Due Dates** - Track deadlines and get reminders
- **Status Tracking** - Pending, In Progress, Completed states
- **Categories & Tags** - Organize tasks with custom categories
- **Estimated vs Actual Time** - Compare planned vs actual time spent
- **Activity Logging** - Complete audit trail of task changes

### 🔗 Current Integrations (Implemented)
- **Azure DevOps** - Work items, boards, and time tracking
- **GitHub** - Issues and project boards
- **Real-time Sync** - Bidirectional synchronization with external systems
- **Status Updates** - Get notified when external tasks change status
- **Work Item Linking** - Link local tasks to external work items

### 📊 Reports & Analytics (Implemented)
- **Comprehensive Reports** - Time logs by task, date range, and category
- **Daily/Weekly Summaries** - Detailed time allocation breakdowns
- **Productivity Insights** - Peak hours, completion rates, and efficiency metrics
- **Data Export** - Export data in multiple formats (CSV, JSON, XML)

## 🚀 Quick Start

### Installation

#### Windows - Instalação Automática (Recomendada)

**Opção 1: Instalador MSI (Recomendado)**
```powershell
# Baixe e execute o instalador MSI para sua arquitetura:
# - Timekeeper-1.0.0-x64.msi (64-bit Intel/AMD)
# - Timekeeper-1.0.0-x86.msi (32-bit Intel/AMD) 
# - Timekeeper-1.0.0-arm64.msi (ARM 64-bit)
```

**Opção 2: Pacote Portátil**
```powershell
# Baixe, extraia e execute:
# - Timekeeper-1.0.0-x64-portable.zip
# - Timekeeper-1.0.0-x86-portable.zip
# - Timekeeper-1.0.0-arm64-portable.zip
```

**Opção 3: Build Local**
```powershell
# Interface simples
cd installers\windows
.\BuildMenu.bat

# Ou PowerShell direto
.\Build.ps1                          # Build x64 completo
.\Build.ps1 -Architecture all        # Todas as arquiteturas
.\Build.ps1 -Type msi                # Apenas MSI
.\Build.ps1 -Type portable           # Apenas portátil
```

#### Windows - Métodos Legacy
```powershell
# Scripts originais (ainda funcionam)
.\scripts\install.ps1                # Auto-detecta admin/usuário
.\scripts\install-windows.ps1        # Instalação do sistema
.\scripts\install-user.ps1           # Instalação do usuário
.\scripts\install-dev.ps1            # Instalação de desenvolvimento
```

#### Linux
```bash
# Make installer executable and run
chmod +x scripts/install-linux.sh
sudo ./scripts/install-linux.sh
```

#### macOS
```bash
# Make installer executable and run
chmod +x scripts/install-macos.sh
./scripts/install-macos.sh
```

### Após a Instalação

Depois da instalação, você pode usar o comando `tk` de qualquer lugar:

```bash
# Executar Timekeeper
tk

# O comando estará disponível em todo o sistema
```

### Basic Usage

After installation, the `tk` command will be available system-wide:

```bash
# Start the interactive CLI
tk

# Run as background service
tk daemon

# Check current status
tk status

# Run background service without system tray
tk daemon --no-tray

# Show help
tk --help
```

## 🛠️ Development

### Prerequisites
- .NET 9.0 SDK
- Git

### Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/timekeeper.git
cd timekeeper

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the CLI
cd src/CLI/Timekeeper.CLI
dotnet run

# Run the service
cd src/Service/Timekeeper.Service
dotnet run -- daemon
```

### Project Structure

```
Timekeeper/
├── src/
│   ├── Application/         # Business logic and CQRS handlers
│   ├── CLI/                 # Command-line interface
│   ├── Domain/              # Domain entities and interfaces
│   ├── Infrastructure/      # Data access and external services
│   └── Service/             # Background service and system integration
├── scripts/                 # Installation and maintenance scripts
└── tests/                   # Unit and integration tests
```

## 📖 Usage Guide

### Starting the Background Service

The background service provides automatic monitoring and notifications:

```bash
# Start the service with system tray (Windows)
tk daemon

# Start without system tray
tk daemon --no-tray

# Start with verbose logging
tk daemon --verbose
```

### Using the CLI Interface

```bash
# Start the interactive CLI
tk start

# Or simply
tk
```

The CLI provides:
- **📝 Manage Tasks** - Create, edit, and organize your tasks
- **⏱️ Time Tracking** - Start/stop timers and add manual time
- **📊 Reports & Insights** - View time reports and productivity insights
- **🔗 DevOps Sync** - Sync with external development tools
- **⚙️ Configuration** - Configure integrations and preferences

### System Tray Features (Windows)

Right-click the system tray icon for quick access to:
- Start/stop timers
- View current status
- Open CLI interface
- Exit the service

### Notifications

Timekeeper will notify you about:
- **⏰ Long-running timers** (4+ hours)
- **☕ Break suggestions** (every 2 hours)
- **🚨 Overdue tasks**
- **📅 Tasks due soon**
- **📥 External updates** from DevOps systems
- **✅ Task completions**

## 🔧 Configuration

### Environment Variables

- `TK_HOME` - Installation directory (set automatically)
- `TIMEKEEPER_DB` - Custom database location (optional)
- `TIMEKEEPER_LOG_LEVEL` - Logging level (Debug, Info, Warning, Error)

### DevOps Integration

Configure external integrations through the CLI:
1. Run `tk` to start the CLI
2. Go to "⚙️ Configuration"
3. Select "🔗 DevOps Integration"
4. Follow the setup wizard for your platform

**Currently Supported:**
- **Azure DevOps** - Work items, boards, and time tracking
- **GitHub** - Issues and project boards

**Coming Soon:**
- **GitLab** - Issues and merge requests
- **Atlassian (Jira/Confluence)** - Issues, projects, and time logging
- **BitBucket** - Pull requests and issues

## 🚀 Roadmap

### 🔗 Integrations & Connectivity (Phase 1)
- [ ] **Atlassian Integration** - Jira and Confluence support with advanced workflow management
- [ ] **GitLab Integration** - Issues, merge requests, and pipeline tracking
- [ ] **BitBucket Integration** - Pull requests, issues, and repository management
- [ ] **Git Analysis** - Examine commits, branches, changesets related to tasks
  - Automatic time tracking based on commit frequency
  - Link commits to specific tasks
  - Branch lifecycle tracking

### 🤖 AI-Powered Features (Phase 2)
- [ ] **AI Commit Summaries** - Generate intelligent summaries of commits, branches, and changesets
- [ ] **Multi-Provider AI Agents** - Support for multiple AI providers:
  - Anthropic (Claude)
  - Google (Gemini)
  - Meta (Llama)
  - xAI (Grok)
  - OpenAI (GPT)
  - Microsoft (Azure OpenAI)
  - DeepSeek
- [ ] **Smart Task Comments** - AI-generated comments and insights for integrated tasks
- [ ] **Productivity AI Assistant** - Intelligent suggestions for time management and task optimization

### 📅 Calendar & Meeting Integration (Phase 3)
- [ ] **Calendar Sync** - Integration with major calendar providers:
  - Google Calendar
  - Microsoft Outlook/365
  - Slack Calendar
  - Zoom Scheduler
- [ ] **Meeting Time Tracking** - Automatic time logging from:
  - Microsoft Teams call history
  - Google Meet sessions
  - Discord voice/video calls
  - Slack huddles and calls
  - Zoom meetings
- [ ] **Smart Meeting Analysis** - AI-powered meeting summaries and time allocation

### 🔄 Advanced Automation (Phase 4)
- [ ] **Webhook System** - Real-time notifications and data export
  - Custom webhook endpoints
  - Scheduled data exports
  - Integration with external monitoring systems
- [ ] **Project Management** - Advanced project organization
  - Multi-task projects with mixed providers
  - Project-level time tracking and reporting
  - Cross-provider task dependencies

### 👥 Collaboration Features (Phase 5)
- [ ] **Team Management** - Multi-user support with role-based access
  - Admin, Developer, Support, and custom roles
  - Team-wide time tracking and reporting
  - Collaborative task management
- [ ] **Organization Support** - Enterprise-level features
  - Multi-team organizations
  - Centralized configuration management
  - Advanced security and compliance

### 🌐 Web Platform (Phase 6)
- [ ] **Web Application** - Full-featured web interface
  - All CLI features in a modern web UI
  - Real-time collaboration
  - Advanced dashboards and analytics
  - Mobile-responsive design

### 🔌 API & Integration Platform (Phase 7)
- [ ] **Comprehensive APIs** - Full programmatic access
  - User-level API for personal automation
  - Organization-level API for enterprise integration
  - Webhook subscriptions and real-time events
  - GraphQL and REST endpoints

### 🐳 Deployment & DevOps (Phase 8)
- [ ] **Docker Containers** - Containerized deployments
  - CLI container for CI/CD pipelines
  - Web application container
  - Background service container
  - Database migration containers
  - Multi-architecture support (AMD64, ARM64)

---

## 🔄 Service Management

### Windows

```bash
# Install as Windows Service
sc create "TimekeeperService" binPath="C:\Program Files\Timekeeper\tk.exe daemon"

# Start service
sc start TimekeeperService

# Stop service
sc stop TimekeeperService

# Remove service
sc delete TimekeeperService
```

### Linux (systemd)

```bash
# Enable service
sudo systemctl enable timekeeper

# Start service
sudo systemctl start timekeeper

# Check status
sudo systemctl status timekeeper

# View logs
sudo journalctl -u timekeeper -f
```

### macOS (LaunchAgent)

```bash
# Load service
launchctl load ~/Library/LaunchAgents/com.timekeeper.daemon.plist

# Start service
launchctl start com.timekeeper.daemon

# Stop service
launchctl stop com.timekeeper.daemon

# Unload service
launchctl unload ~/Library/LaunchAgents/com.timekeeper.daemon.plist
```

## 📊 Reports and Analytics

Timekeeper provides comprehensive reporting:

### Time Logs
- **By Task** - Detailed time breakdown per task
- **By Date Range** - Custom date range analysis
- **Daily Summary** - Today's time allocation
- **Weekly Summary** - Week overview with daily breakdowns

### Productivity Insights
- Time distribution by priority
- Completion rates by category
- Estimated vs actual time analysis
- Peak productivity hours

## 🔒 Data and Privacy

- **Local Storage** - All data stored locally in SQLite database
- **No Cloud Dependency** - Works completely offline
- **Secure Integrations** - External API credentials stored securely
- **Data Export** - Export your data anytime in multiple formats

## 🆘 Troubleshooting

### Common Issues

#### "tk command not found"
- Restart your terminal
- Check if installation completed successfully
- Verify PATH environment variable includes installation directory

#### Service won't start
- Check logs: `tk daemon --verbose`
- Ensure no other instance is running
- Verify database permissions

#### Notifications not working
- **Linux**: Install `libnotify-bin` package
- **macOS**: Check notification permissions in System Preferences
- **Windows**: Ensure Windows notifications are enabled

### Logs

- **Windows**: `%TK_HOME%\logs\`
- **Linux**: `/var/log/timekeeper/` or `journalctl -u timekeeper`
- **macOS**: `~/.timekeeper/logs/`

## 🔧 Uninstallation

```bash
# Run the uninstaller
./scripts/uninstall.sh

# Follow prompts to remove data (optional)
```

## 🤝 Contributing

We welcome contributions to Timekeeper! Here's how you can help:

### Current Focus Areas
- Testing and bug reports for existing features
- Documentation improvements
- Cross-platform compatibility testing
- Performance optimizations

### Upcoming Development
- AI integration features (Phase 2)
- Calendar and meeting integrations (Phase 3)
- Web platform development (Phase 6)

### How to Contribute
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests if applicable
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Submit a pull request

### Development Setup
```bash
# Clone your fork
git clone https://github.com/yourusername/timekeeper.git
cd timekeeper

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Install for development
./scripts/install-dev.ps1
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with .NET 9 and Entity Framework Core
- CLI powered by Spectre.Console for beautiful terminal interfaces
- Cross-platform notifications with platform-specific implementations
- System tray integration using native Windows APIs
- SQLite for local data storage and migrations
- Advanced installer system with WiX Toolset for Windows
- Multi-architecture support (x64, x86, ARM64)

### Special Thanks
- Microsoft .NET team for excellent cross-platform runtime
- Spectre.Console contributors for amazing CLI framework
- SQLite team for robust embedded database
- All beta testers and early adopters

### Technology Stack
- **Backend**: .NET 9, Entity Framework Core, SQLite
- **CLI**: Spectre.Console, System.CommandLine
- **Cross-Platform**: .NET runtime with native integrations
- **Packaging**: WiX Toolset (Windows), Native installers (Linux/macOS)
- **Architecture**: Clean Architecture with CQRS pattern

---

**Timekeeper** - Making time management effortless and productive! ⏰✨

*Current Version: 1.0.0 | Next Major Release: 2.0.0 (Web Platform)*
