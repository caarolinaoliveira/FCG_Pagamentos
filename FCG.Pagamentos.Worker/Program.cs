using FCG.Pagamentos.Application.Configuration;
using FCG.Pagamentos.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.Run();