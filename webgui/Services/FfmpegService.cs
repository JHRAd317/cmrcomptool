using System.Collections.Concurrent;
using System.Text;
using CmrCompTool.WebGui.Models;

namespace CmrCompTool.WebGui.Services;

public class FfmpegService
{
    private static readonly TimeSpan JobRetention = TimeSpan.FromHours(1);

    private readonly ILogger<FfmpegService> _logger;
    private readonly ConcurrentDictionary<string, (JobStatus Status, DateTimeOffset Created)> _jobs = new();

    public FfmpegService(ILogger<FfmpegService> logger)
    {
        _logger = logger;
    }

    public JobStatus StartCompress(string inputPath, string outputPath, int videoKbps, double? durationSeconds = null)
    {
        PurgeOldJobs();

        var jobId = Guid.NewGuid().ToString("N");
        var status = new JobStatus { JobId = jobId };
        _jobs[jobId] = (status, DateTimeOffset.UtcNow);

        _ = Task.Run(async () =>
        {
            try
            {
                await RunFfmpegAsync(inputPath, outputPath, videoKbps, status, durationSeconds);
                status.State = JobState.Completed;
                status.Percent = 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ffmpeg job {JobId} failed", jobId);
                status.State = JobState.Failed;
                AppendLog(status, $"ERROR: {ex.Message}");
            }
        });

        return status;
    }

    public JobStatus? GetJob(string jobId)
        => _jobs.TryGetValue(jobId, out var entry) ? entry.Status : null;

    private void PurgeOldJobs()
    {
        var cutoff = DateTimeOffset.UtcNow - JobRetention;
        foreach (var kvp in _jobs)
        {
            if (kvp.Value.Created < cutoff && kvp.Value.Status.State != JobState.Running)
                _jobs.TryRemove(kvp.Key, out _);
        }
    }

    private async Task RunFfmpegAsync(string inputPath, string outputPath, int videoKbps, JobStatus status, double? durationSeconds)
    {
        // Build ffmpeg args: H.264 + AAC MP4 with bitrate-based video
        var arguments = new[]
        {
            "-y",
            "-i", inputPath,
            "-c:v", "libx264",
            "-b:v", $"{videoKbps}k",
            "-maxrate", $"{videoKbps}k",
            "-bufsize", $"{videoKbps * 2}k",
            "-preset", "medium",
            "-c:a", "aac",
            "-b:a", "128k",
            "-movflags", "+faststart",
            outputPath
        };

        _logger.LogInformation("Starting ffmpeg job {JobId}", status.JobId);

        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in arguments)
            process.StartInfo.ArgumentList.Add(arg);

        // ffmpeg logs to stderr; capture it for progress
        var logLines = new Queue<string>();

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "ffmpeg not found in PATH. Please install ffmpeg and ensure it is accessible.", ex);
        }

        // Read stderr asynchronously
        var readTask = Task.Run(async () =>
        {
            var sb = new StringBuilder();
            var buffer = new char[256];
            while (!process.StandardError.EndOfStream)
            {
                int read = await process.StandardError.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0) break;
                sb.Append(buffer, 0, read);

                // Process complete lines
                var text = sb.ToString();
                var newlineIdx = text.LastIndexOf('\n');
                if (newlineIdx < 0) newlineIdx = text.LastIndexOf('\r');
                if (newlineIdx >= 0)
                {
                    var lines = text[..newlineIdx].Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        lock (logLines)
                        {
                            logLines.Enqueue(line);
                            while (logLines.Count > 50) logLines.Dequeue();
                        }
                        // Parse "time=HH:MM:SS.ss" from ffmpeg progress lines
                        if (durationSeconds.HasValue && durationSeconds.Value > 0)
                        {
                            var timeMatch = System.Text.RegularExpressions.Regex.Match(
                                line, @"time=(\d+):(\d+):(\d+(?:\.\d+)?)");
                            if (timeMatch.Success)
                            {
                                var elapsed = int.Parse(timeMatch.Groups[1].Value) * 3600.0
                                            + int.Parse(timeMatch.Groups[2].Value) * 60.0
                                            + double.Parse(timeMatch.Groups[3].Value,
                                                System.Globalization.CultureInfo.InvariantCulture);
                                var pct = (int)Math.Min(99, Math.Round(elapsed / durationSeconds.Value * 100));
                                status.Percent = pct;
                            }
                        }
                    }
                    sb.Clear();
                    sb.Append(text[(newlineIdx + 1)..]);
                }

                // Update log tail
                lock (logLines)
                {
                    status.LogTail = string.Join("\n", logLines);
                }
            }

            // Flush remaining
            var remaining = sb.ToString().Trim();
            if (!string.IsNullOrEmpty(remaining))
            {
                lock (logLines)
                {
                    logLines.Enqueue(remaining);
                    while (logLines.Count > 50) logLines.Dequeue();
                    status.LogTail = string.Join("\n", logLines);
                }
            }
        });

        await readTask;
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode}. See log for details.");
    }

    private static void AppendLog(JobStatus status, string message)
    {
        var current = status.LogTail;
        var lines = string.IsNullOrEmpty(current)
            ? new List<string>()
            : new List<string>(current.Split('\n'));
        lines.Add(message);
        if (lines.Count > 50) lines.RemoveAt(0);
        status.LogTail = string.Join("\n", lines);
    }
}
