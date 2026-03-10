using CmrCompTool.WebGui.Models;
using CmrCompTool.WebGui.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DownloadsService>();
builder.Services.AddSingleton<FfmpegService>();
builder.Services.AddSingleton<FfprobeService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// ── GET /api/downloads ─────────────────────────────────────────────────────
app.MapGet("/api/downloads", (DownloadsService dl) =>
    Results.Ok(new { path = dl.DownloadsPath }));

// ── GET /api/files ─────────────────────────────────────────────────────────
app.MapGet("/api/files", (DownloadsService dl,
    string? ext,
    double minMB = 0) =>
{
    var extensions = string.IsNullOrWhiteSpace(ext)
        ? new[] { ".mp4", ".wmv", ".avi", ".mov", ".mkv" }
        : ext.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var downloadsPath = dl.DownloadsPath;
    if (!Directory.Exists(downloadsPath))
        return Results.Ok(Array.Empty<FileEntry>());

    var files = Directory.EnumerateFiles(downloadsPath)
        .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
        .Select(f =>
        {
            var info = new FileInfo(f);
            return new FileEntry(info.Name, info.FullName, Math.Round(info.Length / (1024.0 * 1024.0), 2));
        })
        .Where(fe => fe.SizeMB >= minMB)
        .OrderBy(fe => fe.Name)
        .ToArray();

    return Results.Ok(files);
});

// ── POST /api/probe ────────────────────────────────────────────────────────
app.MapPost("/api/probe", async (ProbeRequest req, DownloadsService dl, FfprobeService ffprobe) =>
{
    try
    {
        var safePath = dl.ResolveSafe(req.Path);
        if (!File.Exists(safePath))
            return Results.NotFound(new { error = $"File not found: {safePath}" });

        var result = await ffprobe.ProbeAsync(safePath);
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// ── POST /api/calc ─────────────────────────────────────────────────────────
app.MapPost("/api/calc", (CalcRequest req) =>
{
    if (req.SizeBytes <= 0 || req.TotalBitrate_bps <= 0)
        return Results.BadRequest(new { error = "sizeBytes and totalBitrate_bps must be positive." });

    var sizeMB = (long)Math.Ceiling(req.SizeBytes / (1024.0 * 1024.0));
    var scaleCalc = (double)req.TargetMB / sizeMB;
    var inTotalKbps = (long)Math.Floor(req.TotalBitrate_bps / 1000.0);
    var outVideoKbps = (long)Math.Floor(inTotalKbps * scaleCalc);

    return Results.Ok(new CalcResult(sizeMB, scaleCalc, inTotalKbps, outVideoKbps));
});

// ── POST /api/compress ─────────────────────────────────────────────────────
app.MapPost("/api/compress", (CompressRequest req, DownloadsService dl, FfmpegService ffmpeg) =>
{
    try
    {
        var safeInput = dl.ResolveSafe(req.InputPath);
        if (!File.Exists(safeInput))
            return Results.NotFound(new { error = $"Input file not found: {safeInput}" });

        var safeOutput = dl.ResolveSafe(req.OutputPath);

        if (req.VideoKbps <= 0)
            return Results.BadRequest(new { error = "videoKbps must be positive." });

        var status = ffmpeg.StartCompress(safeInput, safeOutput, req.VideoKbps);
        return Results.Ok(new { jobId = status.JobId });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ── GET /api/jobs/{jobId} ──────────────────────────────────────────────────
app.MapGet("/api/jobs/{jobId}", (string jobId, FfmpegService ffmpeg) =>
{
    var status = ffmpeg.GetJob(jobId);
    if (status == null)
        return Results.NotFound(new { error = $"Job '{jobId}' not found." });

    return Results.Ok(new
    {
        jobId = status.JobId,
        state = status.State.ToString().ToLowerInvariant(),
        percent = status.Percent,
        logTail = status.LogTail
    });
});

app.Run();
