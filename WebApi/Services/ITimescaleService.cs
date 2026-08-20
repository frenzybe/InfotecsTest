using WebApi.Common;
using WebApi.Models;

namespace WebApi.Services;

public interface ITimescaleService
{
    Task<Result> ProcessAndSaveCsvAsync(Stream fileStream, string fileName, CancellationToken ct);
    Task<List<FileResult>> GetFilteredResultsAsync(ResultFilterDto filter, CancellationToken ct);
    Task<List<ValueRecord>> GetLast10ValuesAsync(string fileName, CancellationToken ct);
}