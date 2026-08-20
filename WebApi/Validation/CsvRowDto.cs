namespace WebApi.Validation;

public record CsvRowDto(
    DateTime Date,
    double ExecutionTime,
    double Value,
    int LineNumber
    );