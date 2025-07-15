# Timekeeper Command Reference

Timekeeper is a comprehensive time management and productivity CLI tool that helps you organize tasks, track time, and integrate with external providers.

## Table of Contents
- [Getting Started](#getting-started)
- [Todo Commands](#todo-commands)
- [Time Tracking Commands](#time-tracking-commands)
- [Report Commands](#report-commands)
- [Provider Integration Commands](#provider-integration-commands)
- [Configuration Commands](#configuration-commands)
- [Interactive Mode](#interactive-mode)

## Getting Started

### Basic Usage
```bash
# Start interactive mode (default)
timekeeper
timekeeper interactive

# Show help
timekeeper --help

# Show version and about information
timekeeper about
```

## Todo Commands

### Create Todo Items
```bash
# Basic todo creation
timekeeper todo add --title "Complete project documentation"

# Todo with description and priority
timekeeper todo add --title "Review code" --description "Review PR #123" --priority High

# Todo with due date and category
timekeeper todo add --title "Team meeting" --due-date 2025-01-20 --category "Meetings"

# Todo with tags
timekeeper todo add --title "Bug fix" --tags "urgent,frontend,bug"
```

### List and View Todos
```bash
# List all todos (default limit: 10)
timekeeper todo list

# List with filters
timekeeper todo list --status Pending --limit 20
timekeeper todo list --category "Development" --limit 5

# List specific status
timekeeper todo list --status Completed
timekeeper todo list --status InProgress
```

### Update Todos
```bash
# Mark todo as completed
timekeeper todo complete --id 1

# Update todo properties
timekeeper todo update --id 1 --title "New title"
timekeeper todo update --id 1 --priority High --category "Urgent"
timekeeper todo update --id 1 --tags "updated,important"

# Delete todo
timekeeper todo delete --id 1
```

## Time Tracking Commands

### Basic Time Tracking
```bash
# Start tracking time for a todo
timekeeper time start --todo-id 1

# Start with description
timekeeper time start --todo-id 1 --description "Working on feature implementation"

# Stop current timer
timekeeper time stop

# Stop specific timer
timekeeper time stop --entry-id 5
```

### Manual Time Entries
```bash
# Add manual time entry
timekeeper time add --todo-id 1 --start "2025-01-15 09:00" --end "2025-01-15 12:30"

# With description
timekeeper time add --todo-id 1 --start "2025-01-15 14:00" --end "2025-01-15 16:00" --description "Code review session"
```

### View Time Entries
```bash
# List recent time entries (default: 7 days)
timekeeper time list

# List for specific date
timekeeper time list --date 2025-01-15

# List for custom period
timekeeper time list --days 14

# Show active timers
timekeeper time active
```

## Report Commands

### Generate Reports
```bash
# Daily report for today
timekeeper report daily

# Daily report for specific date
timekeeper report daily --date 2025-01-15

# Weekly report
timekeeper report weekly

# Weekly report for specific week
timekeeper report weekly --start 2025-01-13

# Time summary
timekeeper report summary

# Custom time summary
timekeeper report summary --days 60

# Calendar views - NEW!
# Daily calendar view (detailed timeline)
timekeeper report calendar-day

# Daily calendar for specific date
timekeeper report calendar-day --date 2025-01-15

# Weekly calendar view (grid with daily summaries)
timekeeper report calendar-week

# Weekly calendar for specific week
timekeeper report calendar-week --start 2025-01-13

# Monthly calendar view (full month grid with visual indicators)
timekeeper report calendar-month

# Monthly calendar for specific month
timekeeper report calendar-month --month 2025-01-01
```

## Provider Integration Commands

### Manage Providers
```bash
# List all configured providers
timekeeper provider list

# Show provider status
timekeeper provider status

# Sync all providers
timekeeper provider sync

# Open interactive provider management
timekeeper provider manage
```

### Supported Providers
- **Azure DevOps** - Sync work items with todos
- **GitHub** - Import issues as todo items
- **More providers coming soon...**

## Configuration Commands

### Manage Settings
```bash
# Show current configuration
timekeeper config show

# Set configuration values
timekeeper config set --key "theme" --value "dark"
timekeeper config set --key "default-priority" --value "Medium"
```

## Interactive Mode

When you run `timekeeper` without arguments, you enter interactive mode with a beautiful, navigable menu system:

### Main Menu Options
1. **📋 Manage Todo Items** - Create, edit, and organize tasks
2. **⏱️ Time Tracking** - Start/stop timers and manage time entries
3. **📊 View Reports** - Generate productivity reports
4. **⚙️ Configuration** - Customize settings
5. **🔄 Sync with Providers** - Manage external integrations
6. **ℹ️ About** - Information about Timekeeper

### Navigation Features
- **Arrow Keys** - Navigate menu items
- **Enter** - Select item
- **Escape** - Go back or exit search
- **Search** - Type to filter menu items
- **Backspace** - Clear search

### Interactive Features
- 🔍 **Smart Search** - Filter options by typing
- 🎨 **Beautiful UI** - Colored, styled interface
- 📱 **Responsive** - Adapts to terminal size
- ⌨️ **Keyboard Shortcuts** - Efficient navigation

## Advanced Usage

### Calendar Report Features

The new calendar reports provide visual representations of your work activities:

**Daily Calendar View:**
- Detailed timeline showing exact start/end times
- Activity descriptions and categories
- Duration tracking and active timer indicators
- Task completion summary

**Weekly Calendar View:**
- Grid layout showing 7 days at a glance
- Daily hour totals and task counts
- Visual productivity chart
- Weekend highlighting
- Main activity categories per day

**Monthly Calendar View:**
- Full month grid with color-coded productivity indicators
- 🟢 High productivity days (8+ hours)
- 🟡 Medium productivity days (4-8 hours)  
- 🔴 Low productivity days (1-4 hours)
- Monthly statistics and category breakdowns
- Working day coverage analysis

### Combining Commands
```bash
# Create todo and immediately start tracking
timekeeper todo add --title "Debug issue #456" --tags "bug,urgent"
# Note the ID from output, then:
timekeeper time start --todo-id <id> --description "Investigating root cause"
```

### Provider Workflow
```bash
# 1. Set up provider integration (interactive)
timekeeper provider manage

# 2. Sync to import work items
timekeeper provider sync

# 3. Start working on imported items
timekeeper time start --todo-id <imported-id>

# 4. Generate reports
timekeeper report daily
```

### Productivity Tips
1. **Use Categories** - Organize todos by project or type
2. **Tag Effectively** - Use consistent tagging for better filtering
3. **Regular Syncing** - Keep provider integrations up to date
4. **Daily Reports** - Review your progress each day
5. **Time Tracking** - Track all work for accurate productivity metrics

## Example Workflows

### Daily Workflow
```bash
# Morning: Check today's tasks
timekeeper todo list --status Pending

# Start working
timekeeper time start --todo-id 1

# Lunch break (stop timer)
timekeeper time stop

# Resume after lunch
timekeeper time start --todo-id 1

# End of day: Review progress
timekeeper report daily

# Mark completed tasks
timekeeper todo complete --id 1
```

### Integration Workflow
```bash
# Set up GitHub integration
timekeeper provider manage
# Follow prompts to add GitHub provider

# Sync issues to todos
timekeeper provider sync

# Work on GitHub issue
timekeeper todo list --tags "GitHub"
timekeeper time start --todo-id <github-issue-id>

# Generate weekly report
timekeeper report weekly
```

## Troubleshooting

### Common Issues

**Provider Sync Fails**
```bash
# Check provider status
timekeeper provider status

# Re-configure in interactive mode
timekeeper provider manage
```

**Time Tracking Issues**
```bash
# Check active timers
timekeeper time active

# Stop all timers if needed
timekeeper time stop
```

**Database Issues**
- The application uses SQLite database stored locally
- Configuration and data are preserved between sessions
- Backup your `timekeeper.db` file regularly

### Getting Help
- Use `--help` with any command for specific options
- Enter interactive mode for guided workflows
- Check the About section for version and creator information

---

**Created by [andrrff](https://github.com/andrrff)**  
Built with ❤️ using .NET, Entity Framework, MediatR, and Spectre.Console
