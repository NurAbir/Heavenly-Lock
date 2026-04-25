<p align="center">
  <img src="HeavenlyLock/Assets/logo.png" width="200" alt="HeavenlyLock Logo"/>
</p>

<h1 align="center">HeavenlyLock - User Manual</h1>
<p align="center">Version 1.0.0</p>

---

## Table of Contents

1. [Installation](#1-installation)
2. [First Launch - Creating Your Vault](#2-first-launch---creating-your-vault)
3. [Logging In](#3-logging-in)
4. [Managing Password Entries](#4-managing-password-entries)
5. [Using the Password Generator](#5-using-the-password-generator)
6. [Changing Your Master Password](#6-changing-your-master-password)
7. [Recovering Your Vault](#7-recovering-your-vault)
8. [Lockout Protection](#8-lockout-protection)
9. [Backing Up Your Vault](#9-backing-up-your-vault)
10. [Uninstalling](#10-uninstalling)
11. [Security Notes](#11-security-notes)
12. [Troubleshooting](#12-troubleshooting)

---

## 1. Installation

### Installer (Recommended)

1. Download `HeavenlyLock-Setup.exe` from the [Releases page](../../releases/latest).
2. Run the installer. Windows may show a SmartScreen prompt — click **More info** then **Run anyway** (this is normal for new unsigned software).
3. Follow the setup wizard. By default HeavenlyLock installs to `C:\Program Files\HeavenlyLock\`.
4. A Start Menu shortcut is created automatically. An optional Desktop shortcut can be added during setup.
5. Launch HeavenlyLock from the Start Menu or Desktop.

### Portable

1. Download `HeavenlyLock-Portable.exe` from the [Releases page](../../releases/latest).
2. Place it anywhere you like — a local folder, a USB drive, a network share.
3. Double-click to run. No installation required.

> **Note:** Both versions store vault data in the same location:
> `%LocalAppData%\HeavenlyLock\vault.heavenly`
> So you can switch between installer and portable versions without losing your data.

---

## 2. First Launch - Creating Your Vault

The first time you open HeavenlyLock, no vault exists yet. You will be prompted to create one.

### Step 1 - Choose a master password

Your master password is the single key that protects everything inside HeavenlyLock. Choose wisely:

- Minimum 8 characters (longer is strongly recommended)
- Use a mix of uppercase, lowercase, numbers, and symbols
- Do **not** reuse a password you use anywhere else
- Consider a long passphrase (e.g. `correct-horse-battery-staple`) — long and memorable beats short and complex

Type your chosen password into the **Master Password** field and click **Create Vault**.

### Step 2 - Save your recovery phrase

After the vault is created, HeavenlyLock displays a **12-word recovery phrase**. This is the only way to recover your vault if you forget your master password.

**Write it down on paper and store it somewhere safe — offline.**

- Click **Copy Phrase** to copy it to your clipboard, then paste it into a secure document or write it by hand.
- The phrase is shown **only once**. It cannot be retrieved later.
- Click **Continue** once you have saved it.

> **Warning:** If you lose both your master password and your recovery phrase, your vault data cannot be recovered by anyone. There is no back door.

---

## 3. Logging In

Enter your master password and click **Unlock** (or press **Enter**).

If the password is correct, your vault opens and you land on the dashboard.

### Wrong password

If you enter the wrong password, HeavenlyLock shows the number of attempts remaining before lockout. See [Section 8 - Lockout Protection](#8-lockout-protection) for details.

### Locking the vault

Click the **Lock** button in the top-right of the dashboard at any time. The vault is saved and the master password is cleared from memory. You will be returned to the login screen.

The vault also locks automatically if you close the application.

---

## 4. Managing Password Entries

### Viewing entries

All your saved entries appear in the list on the left side of the dashboard. Click any entry to view and edit its details on the right.

Use the **Search** bar at the top to filter entries by service name, username, or tag.

### Adding an entry

1. Click **+ Add Entry**.
2. A new entry called "New Service" is created and selected automatically.
3. Fill in the fields on the right:
   - **Service** - The name of the website or application (e.g. `GitHub`)
   - **Username** - Your username or email address
   - **Password** - Your password for that service
   - **URL** - The website address (optional)
   - **Notes** - Any additional notes (optional)
   - **Tags** - Comma-separated labels to help with organisation (e.g. `work, email`)
4. Click **Save** to write the changes to disk.

> Changes are held in memory until you click **Save**. Locking the vault also triggers an automatic save.

### Editing an entry

Click any entry in the list to select it, then edit the fields on the right. Click **Save** when done.

### Copying a password

Select an entry and click **Copy Password**. The password is copied to your clipboard.

> The clipboard is automatically cleared after **30 seconds** to prevent accidental exposure.

### Copying a username

Select an entry and click **Copy Username**. No auto-clear applies to usernames.

### Deleting an entry

Select an entry and click **Delete Entry** (or use the delete button in the entry editor). A confirmation dialog appears. Deletion is permanent and saved immediately.

---

## 5. Using the Password Generator

HeavenlyLock includes a built-in password generator accessible from the **Generator** tab in the sidebar.

### Options

| Option | Description |
|---|---|
| **Length** | Slide or type to set the password length (8 to 128 characters) |
| **Uppercase** | Include A-Z |
| **Lowercase** | Include a-z |
| **Numbers** | Include 0-9 |
| **Symbols** | Include special characters such as `!@#$%^&*` |

### Entropy display

The generator shows the estimated **entropy** of the generated password in bits. Higher is better:

| Entropy | Strength |
|---|---|
| Below 40 bits | Weak - do not use |
| 40-60 bits | Fair |
| 60-80 bits | Good |
| 80-100 bits | Strong |
| Above 100 bits | Very strong |

### Generating and using a password

1. Configure the options above.
2. Click **Generate** to produce a new password.
3. Click **Copy** to copy it to your clipboard.
4. Paste it into your new entry's Password field.

---

## 6. Changing Your Master Password

You can change your master password at any time from inside the vault.

1. Click **Change Password** in the dashboard toolbar.
2. Enter your **current master password**.
3. Enter and confirm your **new master password** (minimum 8 characters).
4. Click **Change Password**.

If successful, the vault is immediately re-encrypted with the new password and saved to disk. Your existing entries are not affected.

> **Note:** Changing your master password does **not** change your recovery phrase. The existing recovery phrase remains valid.

---

## 7. Recovering Your Vault

If you forget your master password, you can regain access using your 12-word recovery phrase.

1. On the login screen, click **Forgot Password / Recover**.
2. Enter your 12-word recovery phrase exactly as written (words separated by spaces, all lowercase).
3. Click **Recover**.
4. If the phrase is correct, you will be prompted to set a **new master password**.
5. Enter and confirm your new password, then click **Set New Password**.
6. A **new recovery phrase** is generated. Save it immediately — your old one is now invalid.
7. Click **Continue** to enter the vault.

> **Important:** The recovery phrase is case-insensitive but word order matters. Enter all 12 words in the correct order.

---

## 8. Lockout Protection

HeavenlyLock limits login attempts to protect against brute-force attacks.

- After **10 consecutive failed login attempts**, the vault locks for **10 hours**.
- The lockout countdown is shown on the login screen and continues even if you close and reopen the application.
- Once the lockout expires, you get another 10 attempts.
- A successful login at any point resets the failed attempt counter.

### Locked out and forgotten your password?

Use your recovery phrase — see [Section 7](#7-recovering-your-vault). The recovery phrase bypass is not affected by the login lockout.

---

## 9. Backing Up Your Vault

Your vault is a single encrypted file located at:

```
%LocalAppData%\HeavenlyLock\vault.heavenly
```

To open this folder quickly: press `Win + R`, type `%LocalAppData%\HeavenlyLock` and press Enter.

### Recommended backup strategy

- Copy `vault.heavenly` to an external drive, USB stick, or cloud storage regularly.
- Store your recovery phrase separately from your vault backup (if someone gets both, they can recover your vault).
- After any significant addition of new entries, make a fresh backup.

### Restoring a backup

1. Close HeavenlyLock if it is open.
2. Copy your backed-up `vault.heavenly` file to `%LocalAppData%\HeavenlyLock\`, replacing the existing file.
3. Open HeavenlyLock and log in as normal.

---

## 10. Uninstalling

### Installer version

1. Open **Settings > Apps** (or **Control Panel > Programs**).
2. Find **HeavenlyLock** and click **Uninstall**.

### Portable version

Simply delete `HeavenlyLock-Portable.exe`.

### Removing vault data

Uninstalling the application does **not** delete your vault data. To completely remove everything:

1. Uninstall or delete the application as above.
2. Delete the folder `%LocalAppData%\HeavenlyLock\` manually.

> **Warning:** Deleting the vault folder permanently destroys all stored passwords. Make sure you no longer need them before doing this.

---

## 11. Security Notes

### What is encrypted

- The entire vault, including all entry metadata, is encrypted with **AES-256-GCM**.
- Each individual password is also encrypted separately with its own random nonce.
- The vault file stored on disk contains no plaintext passwords, usernames, or service names.

### How the master password works

Your master password is never stored anywhere. When you log in:

1. Your password is fed into **Argon2id** (a memory-hard key derivation function) along with a random salt to produce a key-encryption-key (KEK).
2. The KEK is used to unwrap the vault's data encryption key (DEK).
3. The DEK is used to decrypt the vault contents.
4. When you lock the vault, the DEK is securely wiped from memory.

This means even if someone obtains your vault file, they cannot read it without knowing your master password.

### Recovery phrase security

The recovery phrase is a second encrypted copy of the DEK, protected by a key derived from the phrase itself. It is stored inside the vault file. Treat it with the same care as your master password.

### Clipboard security

Copied passwords are automatically cleared from the clipboard after 30 seconds. Still, avoid copying passwords on shared or untrusted computers.

---

## 12. Troubleshooting

### "The application won't start"

- Confirm you are running Windows 10 or 11 (x64).
- If using the portable version, make sure your antivirus is not blocking the executable. Single-file .NET executables are sometimes flagged falsely — add an exception if needed.

### "I forgot my master password"

Use your recovery phrase — see [Section 7](#7-recovering-your-vault).

### "I lost both my master password and my recovery phrase"

Your vault data cannot be recovered. There is no back door, reset mechanism, or support override. This is by design — it means no one else can access your data either.

### "The vault file seems corrupted"

Restore from a backup — see [Section 9](#9-backing-up-your-vault). If you have no backup, the data cannot be recovered.

### "Windows SmartScreen is blocking the installer"

This is normal for new software that has not yet built up a reputation with Microsoft. Click **More info** then **Run anyway**. The application does not connect to the internet and contains no malicious code.

### "My antivirus flagged HeavenlyLock"

Single-file self-contained .NET executables are sometimes flagged as suspicious because they unpack themselves to a temp folder on first run. This is normal behaviour for this build type. You can verify the build yourself from the source code.

---

<p align="center">
  HeavenlyLock v1.0.0 -- MIT License<br/>
  <a href="https://github.com/your-username/HeavenlyLock">github.com/your-username/HeavenlyLock</a>
</p>
