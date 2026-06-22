using FCG.Pagamentos.Application.Interfaces;
using FCG.Pagamentos.Domain.Interfaces;
using FCG.Pagamentos.Infrastructure.Context;
using FCG.Pagamentos.Infrastructure.Messaging;
using FCG.Pagamentos.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RabbitMQ.Client;

namespace FCG.Pagamentos.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<PagamentosDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("FCG.Pagamentos.Infrastructure")
                ));

            services.AddScoped<IPagamentoRepository, PagamentoRepository>();

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:Usuario"] ?? "guest",
                    Password = configuration["RabbitMQ:Senha"] ?? "guest"
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton(Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                ));

            services.AddScoped<IMessagePublisher, RabbitMQPublisher>();

            services.AddHostedService<OrderPlacedConsumer>();
            services.AddHostedService<OutboxPublisher>();

            return services;
        }
    }
}