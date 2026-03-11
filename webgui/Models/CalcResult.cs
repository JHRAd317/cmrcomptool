namespace CmrCompTool.WebGui.Models;

public record CalcResult(
    long SizeMB,
    double ScaleCalc,
    long InTotalKbps,
    long OutVideoKbps
);
