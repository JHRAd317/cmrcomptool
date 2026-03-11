using System.Text;
using System.Text.Json;
using CmrCompTool.WebGui.Models;

namespace CmrCompTool.WebGui.Services;

public class FfprobeService
{
    private readonly ILogger<FfprobeService> _logger;

    public FfprobeService(ILogger<FfprobeService> logger)
    {
        _logger = logger;
    }

    public async Task<ProbeResult> ProbeAsync(string filePath, CancellationToken ct = default)
    {
        var args = new[]
        {
            "-v", "error",
            "-print_format", "json",
            "-show_format",
            "-show_streams",
            "--",
            filePath
        };

        _logger.LogInformation("Running ffprobe on {Path}", filePath);

        string output;
        output = await RunProcessAsync("ffprobe", args, ct);

        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;

        var fmt = root.GetProperty("format");
        long sizeBytes = fmt.TryGetProperty("size", out var sizeProp)
            ? long.Parse(sizeProp.GetString()!)
            : 0;
        double duration = fmt.TryGetProperty("duration", out var durProp)
            ? double.Parse(durProp.GetString()!, System.Globalization.CultureInfo.InvariantCulture)
            : 0;
        long? totalBitrate = fmt.TryGetProperty("bit_rate", out var tbrProp)
            ? long.Parse(tbrProp.GetString()!)
            : null;

        int? width = null, height = null;
        long? videoBitrate = null, audioBitrate = null;

        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var stream in streams.EnumerateArray())
            {
                if (!stream.TryGetProperty("codec_type", out var codecType)) continue;
                var type = codecType.GetString();

                if (type == "video" && width == null)
                {
                    width = stream.TryGetProperty("width", out var w) ? w.GetInt32() : null;
                    height = stream.TryGetProperty("height", out var h) ? h.GetInt32() : null;
                    videoBitrate = stream.TryGetProperty("bit_rate", out var vbr)
                        ? long.Parse(vbr.GetString()!)
                        : null;
                }
                else if (type == "audio" && audioBitrate == null)
                {
                    audioBitrate = stream.TryGetProperty("bit_rate", out var abr)
                        ? long.Parse(abr.GetString()!)
                        : null;
                }
            }
        }

        return new ProbeResult(filePath, sizeBytes, duration, width, height, totalBitrate, videoBitrate, audioBitrate);
    }

    private static async Task<string> RunProcessAsync(string exe, IEnumerable<string> arguments, CancellationToken ct)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in arguments)
            process.StartInfo.ArgumentList.Add(arg);

        var sb = new StringBuilder();
        var errorSb = new StringBuilder();

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"{exe} not found in PATH. Please install ffmpeg and ensure it is accessible.", ex);
        }

        process.OutputDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorSb.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{exe} exited with code {process.ExitCode}. stderr: {errorSb}");

        return sb.ToString();
    }
}
