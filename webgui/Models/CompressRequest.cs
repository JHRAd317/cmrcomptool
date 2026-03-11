namespace CmrCompTool.WebGui.Models;

public record CompressRequest(string InputPath, string OutputPath, int VideoKbps, double? DurationSeconds = null);
