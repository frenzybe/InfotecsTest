using FluentValidation;

namespace WebApi.Validation;

public class CsvRowValidator : AbstractValidator<CsvRowDto>
{
    public CsvRowValidator()
    {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Не раньше 01.01.2000[cite: 2]

        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(minDate).WithMessage(x => $"Строка {x.LineNumber}: Дата не может быть раньше 01.01.2000.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage(x => $"Строка {x.LineNumber}: Дата не может быть позже текущей."); // Не позже текущей[cite: 2]

        RuleFor(x => x.ExecutionTime)
            .GreaterThanOrEqualTo(0).WithMessage(x => $"Строка {x.LineNumber}: Время выполнения не может быть меньше 0."); // Не может быть меньше 0[cite: 2]

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0).WithMessage(x => $"Строка {x.LineNumber}: Значение показателя не может быть меньше 0.");
    }
}