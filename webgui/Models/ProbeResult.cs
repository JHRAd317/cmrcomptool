namespace CmrCompTool.WebGui.Models;

public record ProbeResult(
    string Path,
    long SizeBytes,
    double DurationSeconds,
    int? Width,
    int? Height,
    long? TotalBitrate_bps,
    long? VideoBitrate_bps,
    long? AudioBitrate_bps
);
