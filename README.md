<p align="center">
  <img src="HeavenlyLock/Assets/logo.png" width="300" alt="HeavenlyLock Logo"/>
</p>

<h1 align="center">HeavenlyLock</h1>
<p align="center"><b>A secure, fully offline password manager for Windows</b></p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue?style=flat-square"/>
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=flat-square"/>
  <img src="https://img.shields.io/badge/encryption-AES--256--GCM-green?style=flat-square"/>
  <img src="https://img.shields.io/badge/license-MIT-brightgreen?style=flat-square"/>
</p>

---

## What is HeavenlyLock?

HeavenlyLock is a lightweight, fully offline password manager that stores all your credentials in a single encrypted vault file on your own machine. No cloud. No accounts. No telemetry. Your passwords never leave your device.

---

## Features

| Feature | Details |
|---|---|
| **AES-256-GCM encryption** | Vault and every individual entry encrypted separately |
| **Argon2id key derivation** | Resistant to brute-force and GPU attacks |
| **12-word recovery phrase** | Recover access if you forget your master password |
| **Lockout protection** | 10 failed attempts triggers a 10-hour lockout, persisted across restarts |
| **Password generator** | Configurable length, character sets, and live entropy display |
| **Clipboard auto-clear** | Copied passwords are wiped from clipboard after 30 seconds |
| **Search** | Instantly filter entries by service name, username, or tag |
| **Fully offline** | Zero network requests, zero telemetry, zero cloud dependency |

---

## Downloads

Go to [Releases](../../releases/latest) and download:

| File | Description |
|---|---|
| `HeavenlyLock-Setup.exe` | Windows installer (recommended) |
| `HeavenlyLock-Portable.exe` | Single file, no install needed — run from anywhere or a USB drive |

**System requirements:** Windows 10 or 11, x64

---

## Building from Source

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (Windows)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (only needed for the installer step)

### Steps

```powershell
https://github.com/NurAbir/Heavenly-Lock
cd HeavenlyLock

# Build portable EXE + installer in one command
powershell -ExecutionPolicy Bypass -File .\publish.ps1

# Skip the installer step if Inno Setup is not installed
powershell -ExecutionPolicy Bypass -File .\publish.ps1 -SkipInstaller
```

Output lands in `dist\`:
- `HeavenlyLock-Portable.exe`
- `HeavenlyLock-Setup.exe`

---

## Vault Location

Your encrypted vault is stored at:

```
%LocalAppData%\HeavenlyLock\vault.heavenly
```

To back up your vault, copy that file somewhere safe. To restore it, put it back in the same location before launching HeavenlyLock.

---

## Security Overview

- The vault file is encrypted with **AES-256-GCM**. The data encryption key (DEK) is wrapped with a key-encryption-key (KEK) derived from your master password using **Argon2id**.
- Each password entry is encrypted individually with its own random nonce using the vault DEK.
- The master password is **never stored anywhere** — only a derived key is used, and it exists in memory only while the vault is open.
- The 12-word recovery phrase is shown exactly once at vault creation. Store it offline in a safe place.
- After 10 consecutive failed login attempts, the vault locks for **10 hours**. This lockout state survives app restarts.

---

## License

MIT -- see [LICENSE](LICENSE)
