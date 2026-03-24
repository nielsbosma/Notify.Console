# Security Guidelines

## 🔒 Protecting Your Credentials

This is a **public repository**. Never commit secrets, credentials, or personal information.

## ✅ What's Safe (Protected by .gitignore)

The following files are automatically excluded from git:

### Configuration Files
- `config.yaml` - Your actual configuration with real email addresses
- `src/config.yaml` - Alternate location (both are ignored)

### OAuth Credentials
- `client_secret*.json` - OAuth app credentials from Google Cloud
- `user_credential*.json` - Generated access/refresh tokens
- `**/gmail-credentials/` - Entire credentials directory

### Test Files
- `test-*.txt` - Test email files you create
- `test-*.html` - HTML test files you create

### Build Artifacts
- `bin/`, `obj/` - Build output directories
- `nupkgs/` - NuGet packages

## ⚠️ What Gets Committed (Public)

These files **ARE** committed and visible to everyone:

- `example.config.yaml` - Example configuration (no real credentials)
- Source code files (`.cs`, `.csproj`)
- Documentation (`.md` files)
- `.gitignore` itself

## 🔐 OAuth Credential Storage

OAuth credentials are stored **outside the repository** in your user profile:

- **Windows:** `%APPDATA%\Notify.Console\gmail-credentials\{profile}\`
- **Unix:** `~/.config/notify-console/gmail-credentials/{profile}/`

These directories are:
- Outside the git repository
- User-specific
- Never committed to version control
- Secure from accidental commits

## 📋 Setup Checklist

Before committing any changes:

1. ✅ Check `.gitignore` includes `config.yaml`
2. ✅ Use `example.config.yaml` as a template only
3. ✅ Keep OAuth credentials in `%APPDATA%` (Windows) or `~/.config` (Unix)
4. ✅ Run `git status` to verify no secrets are staged
5. ✅ Never commit files with real email addresses or API keys

## 🚨 If You Accidentally Commit Secrets

If you accidentally commit credentials:

1. **Revoke the credentials immediately** at https://console.cloud.google.com
2. **Remove from git history:**
   ```bash
   # Remove file from git but keep locally
   git rm --cached config.yaml
   git commit -m "Remove accidentally committed config"
   ```
3. **For sensitive history**, consider using `git filter-branch` or BFG Repo-Cleaner
4. **Create new credentials** and update your `config.yaml`

## 📖 Best Practices

1. **Always use example.config.yaml as template** - Copy it to `config.yaml`
2. **Never edit example.config.yaml with real data** - Keep it generic
3. **Double-check before commits** - Run `git status` and `git diff --staged`
4. **Use separate Google Cloud projects** - For personal vs work profiles
5. **Rotate credentials periodically** - Delete and recreate OAuth credentials
6. **Limit OAuth scopes** - Only use `gmail.send` scope (not `gmail.modify`)

## 🛡️ Current Protection Status

- ✅ `.gitignore` configured to exclude `config.yaml`
- ✅ OAuth credentials stored in user profile directory (outside repo)
- ✅ Example configuration has placeholder values only
- ✅ Build artifacts excluded
- ✅ Test files excluded

## 📞 Questions?

If you're unsure whether a file should be committed, check:

1. Does it contain real email addresses? → **Don't commit**
2. Does it contain API keys or OAuth credentials? → **Don't commit**
3. Is it personal test data? → **Don't commit**
4. Is it a generic example? → **Safe to commit**

**When in doubt, don't commit it.**
