using FCG.Pagamentos.Application.Interfaces;
using FCG.Pagamentos.Domain.Interfaces;
using FCG.Pagamentos.Infrastructure.Context;
using FCG.Pagamentos.Infrastructure.Logging;
using FCG.Pagamentos.Infrastructure.Messaging;
using FCG.Pagamentos.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
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
            // Lidos aqui (e não dentro das lambdas abaixo) para a aplicação falhar já no startup, e não no primeiro uso.
            var connectionString = ObterObrigatorio(configuration, "ConnectionStrings:DefaultConnection");
            var rabbitUsuario = ObterObrigatorio(configuration, "RabbitMQ:Usuario");
            var rabbitSenha = ObterObrigatorio(configuration, "RabbitMQ:Senha");

            services.AddDbContext<PagamentosDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly("FCG.Pagamentos.Infrastructure")
                ));

            services.AddScoped<IPagamentoRepository, PagamentoRepository>();

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = rabbitUsuario,
                    Password = rabbitSenha
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton(Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                ));

            services.AddSingleton<IMongoClient>(sp =>
                new MongoClient(configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017"));

            services.AddScoped<IMessagePublisher, RabbitMQPublisher>();
            services.AddSingleton<IErroLogService, ErroLogService>();

            services.AddHostedService<OrderPlacedConsumer>();
            services.AddHostedService<OutboxPublisher>();

            return services;
        }

        private static string ObterObrigatorio(IConfiguration configuration, string chave)
        {
            var valor = configuration[chave];

            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException(
                    $"Configuração obrigatória '{chave}' não encontrada. " +
                    $"Defina via 'dotnet user-secrets' (desenvolvimento) ou pela variável de ambiente " +
                    $"'{chave.Replace(":", "__")}' (produção, injetada pelo Azure Key Vault).");

            return valor;
        }
    }
}
