namespace CmrCompTool.WebGui.Models;

public record CalcRequest(long SizeBytes, long TotalBitrate_bps, int TargetMB = 400);
