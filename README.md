# Notify.Console

Cross-platform notification CLI tool, installed as a .NET global tool.

## Install

```bash
dotnet tool install -g Notify.Console
```

## Commands

### system

Show a native OS notification (toast/banner).

```bash
notify system --title "Build done" --description "All tests passed"
```

| Platform | Method |
|----------|--------|
| Windows | Balloon tip via `System.Windows.Forms.NotifyIcon` |
| macOS | `osascript` / `display notification` |
| Linux | `notify-send` |

### messagebox

Show a native OS message box dialog (blocks until dismissed).

```bash
notify messagebox --title "Confirm" --message "Deployment complete"
```

| Platform | Method |
|----------|--------|
| Windows | `System.Windows.Forms.MessageBox` |
| macOS | `osascript` / `display dialog` |
| Linux | `zenity --info` |

### slack

Send a Slack message using a config profile defined in `config.yaml`.

```bash
notify slack my-profile --message "Deploy finished"
```

See `config.yaml` for profile configuration.

### email

Send emails via Gmail API with support for HTML, attachments, and multiple recipients.

**Basic usage:**
```bash
notify email work --to user@example.com --subject "Test" --body "Hello"
```

**Using stdin (recommended for large emails):**
```bash
cat report.html | notify email work --to team@company.com --subject "Weekly Report"
```

**Reading from file:**
```bash
notify email work --to user@example.com --subject "Article" --file article.html
```

**Multiple recipients and attachments:**
```bash
notify email work \
  --to user1@example.com --to user2@example.com \
  --cc boss@example.com \
  --subject "Files" \
  --body "Please review attached documents" \
  --attach document.pdf --attach spreadsheet.xlsx
```

**Create draft instead of sending:**
```bash
cat report.html | notify email work --to team@example.com --subject "Draft Report" --draft
```

**Features:**
- Automatic HTML detection with plain text fallback generation
- Multiple recipients (To, CC, BCC)
- File attachments (up to 25MB Gmail limit)
- Multiple input methods: stdin, `--body`, `--file`
- Draft mode for creating drafts instead of sending
- Profile-based configuration in `config.yaml`

**Gmail API Setup (one-time per profile):**

1. Create a Google Cloud project at https://console.cloud.google.com
2. Enable the Gmail API
3. Create OAuth 2.0 credentials (Desktop app type)
4. Download `client_secret.json`
5. Place the file at:
   - Windows: `%APPDATA%\Notify.Console\gmail-credentials\{profile-name}\client_secret.json`
   - Unix: `~/.config/notify-console/gmail-credentials/{profile-name}/client_secret.json`
6. First run will open browser for OAuth consent
7. Grant Gmail send permission
8. Credentials are saved automatically for future use

**Configuration example (`config.yaml`):**
```yaml
email:
  work:
    provider: gmail
    from: me@company.com
    default_to: team@company.com  # Optional

  personal:
    provider: gmail
    from: personal@gmail.com
```
