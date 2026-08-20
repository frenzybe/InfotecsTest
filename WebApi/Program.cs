using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Endpoints;
using WebApi.Services;
using WebApi.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITimescaleService, TimescaleService>();

builder.Services.AddValidatorsFromAssemblyContaining<CsvRowValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. Включение Swagger в пайплайн
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6. Подключение наших вынесенных эндпоинтов вместо стандартного "Hello World"
app.MapTimescaleEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();