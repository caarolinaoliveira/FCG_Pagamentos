using FCG.Pagamentos.Application.Configuration;
using FCG.Pagamentos.Infrastructure.Configuration;
using FCG.Pagamentos.Infrastructure.Context;
using FCG.Pagamentos.Worker.Logging;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.FormatterName = "compact")
    .AddConsoleFormatter<CompactConsoleFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PagamentosDbContext>();
    db.Database.Migrate();
}

app.Run();
