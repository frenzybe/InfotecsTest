namespace WebApi.Models;

public class ValueRecord
{
    public int Id  { get; set; }
    public string FileName { get; set; } = String.Empty;
    public DateTime Date { get; set; }
    public double ExecutionTime { get; set; }
    public double Value { get; set; }
}