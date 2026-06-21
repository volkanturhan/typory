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
- **English & Turkish** — switch the interface language from the tray.
- **Private by design** — everything stays on your machine; nothing is uploaded.

## Run it

typory isn't published as a prebuilt download yet, so for now you run it from
source. You'll need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(the SDK, not just the runtime) on Windows.

```bash
git clone https://github.com/volkanturhan/typory.git
cd typory
dotnet run --project typory/typory.csproj
```

typory starts quietly in the system tray — **no window pops up**. That's normal;
double-click the tray icon (or use **Manage snippets**) to set up your snippets.

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
resume), **Start with Windows**, language, and **Quit**.

## Where your data lives

Your snippets are stored locally at `%APPDATA%\typory\snippets.json` and never
leave your machine; preferences live next to them in `settings.json`.

## Build a shareable exe

Want a standalone `.exe` you can hand to someone without the SDK? Build it
yourself — the output isn't checked into the repo:

```bash
# Builds into dist/ (self-contained typory.exe + lite build)
pwsh tools/publish.ps1
```

## Tech

- C# / WPF on .NET 8 (Windows)
- No third-party dependencies

## License

MIT — see [LICENSE](LICENSE).
