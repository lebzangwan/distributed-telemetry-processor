using Telemetry.Publisher.Repository;
using Telemetry.Publisher.Services;
using Telemetry.Publisher.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IDatabaseRepository, DatabaseRepository>();
builder.Services.AddSingleton<ITelemetryQueueManager, TelemetryQueueManager>();
builder.Services.AddHostedService<DataGeneratorHostedService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapControllers();

app.Run();