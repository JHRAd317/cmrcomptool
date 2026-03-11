# CMR Compression Toolkit

Local web-based GUI for compressing video files using **ffprobe** and **ffmpeg**.  
Files are read from (and written back to) the current Windows user's **Downloads** folder.

---

## Quick start — pre-built zip (no SDK required)

1. Go to the **[Releases](../../releases)** page and download the latest `CmrCompTool-win-x64.zip`.
2. Extract the zip to any folder (e.g. `C:\Tools\CmrCompTool`).
3. Install **ffmpeg** (once per machine):
   ```
   winget install ffmpeg
   ```
4. Double-click `CmrCompTool.WebGui.exe`.  
   Your browser opens `http://localhost:5000` automatically.

---

## Quick start — from source

**Prerequisites:** [.NET 8 SDK](https://dot.net) + ffmpeg in PATH (see above).

```bat
REM Clone or download the repo, then:
run.bat
```

`run.bat` starts the server and opens the browser automatically.  
Or start manually:

```bat
cd webgui
dotnet run --no-launch-profile --urls http://localhost:5000
```

---

## How it works

| Step | What you do | What happens |
|------|-------------|--------------|
| 1 | Pick a file | Lists `.mp4 .wmv .avi .mov .mkv` from Downloads |
| 2 | Click **Probe** | `ffprobe` reads metadata (size, duration, resolution, bitrates) |
| 3 | Click **Calculate** | Computes recommended output bitrate targeting 400 MB |
| 4 | Click **Compress** | `ffmpeg` encodes H.264+AAC MP4; log lines stream to the page |

See **[webgui/README.md](webgui/README.md)** for full details, API reference, and troubleshooting.

---

## Repository layout

```
cmrcomptool/
├── CMR_Compression_ToolkitOriginal.txt   ← original PowerShell/WinForms script
├── run.bat                               ← Windows one-click launcher (source)
└── webgui/                               ← ASP.NET Core 8 web app
    ├── Program.cs                        ← REST API endpoints
    ├── Services/                         ← ffprobe, ffmpeg, Downloads helpers
    ├── wwwroot/index.html                ← single-page UI
    └── README.md                         ← full setup guide
```
