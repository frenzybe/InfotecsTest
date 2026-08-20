using WebApi.Models;
using WebApi.Services;

namespace WebApi.Endpoints;

public static class TimescaleEndpoints
{
    public static void MapTimescaleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/timescale").WithTags("Timescale Data");

        group.MapPost("/upload", UploadCsvAsync)
            .DisableAntiforgery()
            .WithSummary("Загрузка и парсинг CSV файла");

        group.MapGet("/results", GetResultsAsync)
            .WithSummary("Получение интегральных результатов с фильтрами");

        group.MapGet("/values/{fileName}/last10", GetLast10ValuesAsync)
            .WithSummary("Получить 10 последних значений для файла");
    }

    private static async Task<IResult> UploadCsvAsync(IFormFile file, ITimescaleService service, CancellationToken ct)
    {
        if (file is null || file.Length == 0) 
            return Results.BadRequest("Файл не выбран.");

        using var stream = file.OpenReadStream();
        var result = await service.ProcessAndSaveCsvAsync(stream, file.FileName, ct);

        return result.IsSuccess 
            ? Results.Ok("Файл успешно обработан.") 
            : Results.BadRequest(result.ErrorMessage);
    }

    private static async Task<IResult> GetResultsAsync([AsParameters] ResultFilterDto filter, ITimescaleService service, CancellationToken ct)
    {
        var results = await service.GetFilteredResultsAsync(filter, ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> GetLast10ValuesAsync(string fileName, ITimescaleService service, CancellationToken ct)
    {
        var values = await service.GetLast10ValuesAsync(fileName, ct);
        return values.Count != 0 ? Results.Ok(values) : Results.NotFound();
    }
}