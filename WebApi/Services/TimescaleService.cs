// Services/TimescaleService.cs
using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebApi.Common;
using WebApi.Data;
using WebApi.Models;
using WebApi.Validation;

namespace WebApi.Services;

public class TimescaleService(AppDbContext db, IValidator<CsvRowDto> validator) : ITimescaleService
{
    public async Task<Result> ProcessAndSaveCsvAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        // 1. Парсинг и валидация
        var parseResult = await ParseCsvAsync(fileStream, fileName, ct);
        if (parseResult.IsFailure)
            return Result.Failure(parseResult.ErrorMessage!);

        var (records, fileResult) = parseResult.Value;

        // 2. Сохранение в БД
        return await SaveToDatabaseAsync(fileName, records, fileResult, ct);
    }

    public async Task<List<FileResult>> GetFilteredResultsAsync(ResultFilterDto filter, CancellationToken ct)
    {
        var query = db.Results.AsNoTracking();

        if (!string.IsNullOrEmpty(filter.FileName)) 
            query = query.Where(r => r.FileName.Contains(filter.FileName));
        
        if (filter.MinStartTime.HasValue) query = query.Where(r => r.MinDate >= filter.MinStartTime.Value);
        if (filter.MaxStartTime.HasValue) query = query.Where(r => r.MinDate <= filter.MaxStartTime.Value);
        
        if (filter.MinAvgValue.HasValue) query = query.Where(r => r.AverageValue >= filter.MinAvgValue.Value);
        if (filter.MaxAvgValue.HasValue) query = query.Where(r => r.AverageValue <= filter.MaxAvgValue.Value);
        
        if (filter.MinAvgExecutionTime.HasValue) query = query.Where(r => r.AverageExecutionTime >= filter.MinAvgExecutionTime.Value);
        if (filter.MaxAvgExecutionTime.HasValue) query = query.Where(r => r.AverageExecutionTime <= filter.MaxAvgExecutionTime.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<List<ValueRecord>> GetLast10ValuesAsync(string fileName, CancellationToken ct)
    {
        return await db.Values
            .AsNoTracking()
            .Where(v => v.FileName == fileName)
            .OrderByDescending(v => v.Date)
            .Take(10)
            .ToListAsync(ct);
    }

    // --- Приватные вспомогательные методы (Clean Code) ---

    private async Task<Result<(List<ValueRecord> Records, FileResult Result)>> ParseCsvAsync(Stream stream, string fileName, CancellationToken ct)
    {
        using var reader = new StreamReader(stream);
        _ = await reader.ReadLineAsync(ct); 

        var records = new List<ValueRecord>();
        var valuesForMedian = new List<double>();
        int lineCount = 0;
        
        double sumExecTime = 0, sumValue = 0;
        DateTime minDate = DateTime.MaxValue, maxDate = DateTime.MinValue;
        double minVal = double.MaxValue, maxVal = double.MinValue;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            lineCount++;

            if (lineCount > 10000) return Result<(List<ValueRecord>, FileResult)>.Failure("Количество строк не может быть больше 10 000."); //[cite: 2]

            var span = line.AsSpan();
            int firstSep = span.IndexOf(';');
            int secondSep = span[(firstSep + 1)..].IndexOf(';') + firstSep + 1;

            if (firstSep == -1 || secondSep <= firstSep)
                return Result<(List<ValueRecord>, FileResult)>.Failure($"Ошибка формата в строке {lineCount}."); //[cite: 2]

            if (!DateTime.TryParseExact(span[..firstSep], "yyyy-MM-ddTHH-mm-ss.ffffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date) ||
                !double.TryParse(span[(firstSep + 1)..secondSep], CultureInfo.InvariantCulture, out var execTime) ||
                !double.TryParse(span[(secondSep + 1)..], CultureInfo.InvariantCulture, out var val))
            {
                return Result<(List<ValueRecord>, FileResult)>.Failure($"Ошибка типов данных в строке {lineCount}."); //[cite: 2]
            }

            var rowDto = new CsvRowDto(date, execTime, val, lineCount);
            var validationResult = validator.Validate(rowDto);
            if (!validationResult.IsValid) return Result<(List<ValueRecord>, FileResult)>.Failure(validationResult.Errors.First().ErrorMessage);

            records.Add(new ValueRecord { FileName = fileName, Date = date, ExecutionTime = execTime, Value = val });
            valuesForMedian.Add(val);

            if (date < minDate) minDate = date;
            if (date > maxDate) maxDate = date;
            if (val < minVal) minVal = val;
            if (val > maxVal) maxVal = val;
            sumExecTime += execTime;
            sumValue += val;
        }

        if (lineCount < 1) return Result<(List<ValueRecord>, FileResult)>.Failure("Количество строк не может быть меньше 1."); //[cite: 2]

        valuesForMedian.Sort();
        double median = valuesForMedian.Count % 2 != 0 
            ? valuesForMedian[valuesForMedian.Count / 2] 
            : (valuesForMedian[(valuesForMedian.Count / 2) - 1] + valuesForMedian[valuesForMedian.Count / 2]) / 2.0;

        var fileResult = new FileResult
        {
            FileName = fileName,
            TimeDeltaSeconds = (maxDate - minDate).TotalSeconds,
            MinDate = minDate,
            AverageExecutionTime = sumExecTime / lineCount,
            AverageValue = sumValue / lineCount,
            MedianValue = median,
            MaxValue = maxVal,
            MinValue = minVal
        };

        return Result<(List<ValueRecord>, FileResult)>.Success((records, fileResult));
    }

    private async Task<Result> SaveToDatabaseAsync(string fileName, List<ValueRecord> records, FileResult fileResult, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // Перезаписывать значения в базе[cite: 2]
                await db.Values.Where(v => v.FileName == fileName).ExecuteDeleteAsync(ct);
                await db.Results.Where(r => r.FileName == fileName).ExecuteDeleteAsync(ct);

                await db.Values.AddRangeAsync(records, ct);
                await db.Results.AddAsync(fileResult, ct);

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                
                return Result.Success();
            }
            catch
            {
                await tx.RollbackAsync(ct); // Откатить изменения[cite: 2]
                return Result.Failure("Ошибка при сохранении данных в БД.");
            }
        });
    }
}