# typory

**English | [Türkçe](README.tr.md)**

A lightweight Windows text expander.

typory lives quietly in your system tray and watches for short abbreviations as
you type. The moment you type one — say `;mail` — it deletes it and types the
full text in its place (`volkanturhan@gmail.com`), in whatever app you're using.
Set up the snippets you type over and over once, then never type them in full
again.

<p align="center">
  <img src="docs/screenshot.png" alt="typory's snippet manager" width="420" />
</p>

## Features

- **Expand anywhere** — works system-wide, in any app, as you type.
- **Your snippets** — manage abbreviation → expansion rules in a simple window.
- **Unicode & symbols** — expansions can be anything, e.g. `;shrug` → `¯\_(ツ)_/¯`.
- **Layout-aware** — decodes keys with your keyboard layout, so non-US layouts
  (e.g. Turkish) and AltGr characters work.
- **Pause anytime** — toggle expansion on/off from the tray.
- **Survives restarts** — your snippets are saved and restored.
- **Start with Windows** — optional, toggled from the tray menu.
- **Self-updating** — when a new version ships, typory offers it from the tray; one click installs it.
- **English & Turkish** — switch the interface language from the tray.
- **Dark mode** — System / Dark / Light theme from the tray (follows Windows by default).
- **Private by design** — everything stays on your machine; nothing is uploaded.

## Download

Grab the latest build from the [**Releases**](https://github.com/volkanturhan/typory/releases/latest) page:

- **typory-setup-…exe** — installer (recommended). No admin rights needed, and typory keeps itself up to date from here on.
- **typory-…exe** — portable single file; just run it, nothing to install.

Both are self-contained, so you don't need .NET installed. Windows 10/11, 64-bit.

typory starts quietly in the system tray — **no window pops up**. That's normal;
double-click the tray icon (or use **Manage snippets**) to set up your snippets.

## Run from source

Prefer to build it yourself? You'll need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(the SDK, not just the runtime) on Windows.

```bash
git clone https://github.com/volkanturhan/typory.git
cd typory
dotnet run --project typory/typory.csproj
```

## How to use

1. Launch typory — it starts quietly in the system tray.
2. Double-click the tray icon (or right-click → **Manage snippets**) to open the
   manager. It starts with a few example snippets.
3. Add rows: an **abbreviation** (e.g. `;addr`) and its **expansion** (your
   address). Changes save automatically — there's no Save button.
4. Now just type the abbreviation in any app; typory replaces it instantly.

Tip: start abbreviations with a character you'd never type by accident (like `;`
or `:`) so they only fire when you mean them.

Right-click the tray icon for **Manage snippets**, **Expansion enabled** (pause /
resume), **Start with Windows**, language, **Theme** (System / Dark / Light),
**Check for updates**, and **Quit**.

## Where your data lives

Your snippets are stored locally at `%APPDATA%\typory\snippets.json` and never
leave your machine; preferences live next to them in `settings.json`.

## Build it yourself

Want to produce the release artifacts locally? They aren't checked into the repo:

```bash
# Portable self-contained exe + the Windows installer, into dist/release.
# (The installer step needs Inno Setup: winget install JRSoftware.InnoSetup)
pwsh tools/release.ps1
```

## Tech

- C# / WPF on .NET 8 (Windows)
- No third-party dependencies

## License

MIT — see [LICENSE](LICENSE).
