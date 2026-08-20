namespace WebApi.Models;

public class FileResult
{
    public int Id { get; set; }
    public String FileName { get; set; } = String.Empty;
    public double TimeDeltaSeconds { get; set; }
    public DateTime MinDate { get; set; }
    public double AverageExecutionTime { get; set; }
    public double AverageValue { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double MedianValue { get; set; }
}