using FCG.Pagamentos.Application.Configuration;
using FCG.Pagamentos.Infrastructure.Configuration;
using FCG.Pagamentos.Worker.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.FormatterName = "compact")
    .AddConsoleFormatter<CompactConsoleFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.Run();