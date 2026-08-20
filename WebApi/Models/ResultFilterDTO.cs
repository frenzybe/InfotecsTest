namespace WebApi.Models;

public record ResultFilterDto(
    string? FileName,
    DateTime? MinStartTime,
    DateTime? MaxStartTime,
    double? MinAvgValue,
    double? MaxAvgValue,
    double? MinAvgExecutionTime,
    double? MaxAvgExecutionTime
    );