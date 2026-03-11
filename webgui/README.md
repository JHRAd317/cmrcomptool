# CMR Compression Toolkit – Web GUI

A local ASP.NET Core web application that replaces the original PowerShell/WinForms GUI
with a browser-based interface backed by **ffprobe** and **ffmpeg**.

---

## Option A — Pre-built zip (recommended, no SDK required)

1. Go to the [Releases](../../releases) page and download `CmrCompTool-win-x64.zip`.
2. Extract to any folder (e.g. `C:\Tools\CmrCompTool`).
3. Install ffmpeg (if not already installed — see [Install ffmpeg](#1-install-ffmpeg-includes-ffprobe) below).
4. Double-click `CmrCompTool.WebGui.exe`. The browser opens `http://localhost:5000` automatically.

> The self-contained exe bundles the .NET 8 runtime — no SDK install needed.

---

## Option B — Run from source

### Prerequisites

#### 1. Install ffmpeg (includes ffprobe)

**Windows**
```
winget install ffmpeg
```
or download from <https://ffmpeg.org/download.html> and add the `bin/` folder to your `PATH`.

Verify:
```
ffmpeg -version
ffprobe -version
```

#### 2. Install .NET 8 SDK

Download from <https://dot.net> → Download .NET 8 SDK.

Verify:
```
dotnet --version
# Should print 8.x.x
```

---

## Running the app

**One-click (from repo root):**
```bat
run.bat
```
This starts the server and opens `http://localhost:5000` in your browser automatically.

**Manual:**
```bat
cd webgui
dotnet run
```

The console will print something like:

```
Now listening on: http://localhost:5000
```

Open that URL in your browser.

> **Note:** The app runs as _the same Windows user account_ that launched it.
> It will read and write files in **that user's `%USERPROFILE%\Downloads`** folder.
> Do not run the app as a different user or as a service account unless that account
> has the Downloads folder you intend to use.

---

## Usage workflow

1. **Select source file** – The page lists all video files (`.mp4 .wmv .avi .mov .mkv`)
   found in your Downloads folder. Click *Select* on any row.
2. **Probe** – Click *Probe* to retrieve metadata (size, duration, resolution, bitrates)
   via `ffprobe`.
3. **Calculate** – Enter a target size in MB (default 400 MB) and click *Calculate*.
   The tool computes the recommended output video bitrate using the same scaling formula
   as the original PowerShell script:

   ```
   sizeMB       = Ceiling(sizeBytes / 1 MiB)
   scaleCalc    = targetMB / sizeMB
   inTotalKbps  = Floor(totalBitrate_bps / 1000)
   outVideoKbps = Floor(inTotalKbps × scaleCalc)
   ```

4. **Compress** – The output filename and bitrate are auto-filled
   (`<basename>-DR-<outVideoKbps>.mp4`). Adjust if needed and click *Compress*.
   The page polls for progress and shows recent ffmpeg log lines.

---

## REST API reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/downloads` | Returns the resolved Downloads folder path |
| GET | `/api/files?ext=...&minMB=...` | Lists video files in Downloads |
| POST | `/api/probe` | `{ "path": "..." }` → ffprobe metadata |
| POST | `/api/calc` | `{ "sizeBytes", "totalBitrate_bps", "targetMB" }` → bitrate calculation |
| POST | `/api/compress` | `{ "inputPath", "outputPath", "videoKbps" }` → starts ffmpeg job, returns `{ "jobId" }` |
| GET | `/api/jobs/{jobId}` | Returns job state, percent, and recent log lines |

All path parameters are validated to be inside Downloads (path-traversal protection).
Both bare filenames and full paths under Downloads are accepted.

---

## ffmpeg command used

```
ffmpeg -y -i <input>
       -c:v libx264 -b:v <kbps>k -maxrate <kbps>k -bufsize <2×kbps>k
       -preset medium
       -c:a aac -b:a 128k
       -movflags +faststart
       <output>
```

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `ffprobe not found in PATH` | Install ffmpeg and ensure `ffprobe` is on `PATH` |
| `ffmpeg not found in PATH` | Same as above |
| No files listed | Check that `.mp4` / `.wmv` / etc. files exist in your Downloads folder |
| Output path error | The output filename must stay within Downloads; do not include a different directory |
