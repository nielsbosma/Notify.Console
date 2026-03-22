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
