using Telemetry.Consumer.Services;
using Telemetry.Consumer.Repository;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConsumerRepository, ConsumerRepository>();
builder.Services.AddHttpClient<TelemetryProcessorWorker>();
builder.Services.AddHostedService<TelemetryProcessorWorker>();

var host = builder.Build();
host.Run();