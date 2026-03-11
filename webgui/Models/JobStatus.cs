namespace CmrCompTool.WebGui.Models;

public enum JobState { Running, Completed, Failed }

public class JobStatus
{
    public string JobId { get; init; } = string.Empty;
    public JobState State { get; set; } = JobState.Running;
    public int? Percent { get; set; }
    public string LogTail { get; set; } = string.Empty;
}
