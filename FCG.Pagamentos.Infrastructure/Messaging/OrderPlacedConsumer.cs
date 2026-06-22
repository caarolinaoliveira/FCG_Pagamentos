using System.Text;
using System.Text.Json;
using FCG.Pagamentos.Application.Events;
using FCG.Pagamentos.Application.Interfaces;
using FCG.Pagamentos.Domain.Entities;
using FCG.Pagamentos.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FCG.Pagamentos.Infrastructure.Messaging
{
    public class OrderPlacedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderPlacedConsumer> _logger;
        private readonly IConnection _connection;
        private IChannel? _channel;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

        public OrderPlacedConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderPlacedConsumer> logger,
            IConnection connection,
            AsyncCircuitBreakerPolicy circuitBreaker)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _connection = connection;
            _circuitBreaker = circuitBreaker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync("dlx", ExchangeType.Direct, durable: true);

            await _channel.QueueDeclareAsync(
                queue: "order.placed.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            await _channel.QueueBindAsync("order.placed.dlq", "dlx", "order.placed");

            var args = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", "dlx" },
                { "x-dead-letter-routing-key", "order.placed" }
            };

            await _channel.QueueDeclareAsync(
                queue: "order.placed",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                if (_circuitBreaker.CircuitState == CircuitState.Open)
                {
                    _logger.LogWarning("[PAGAMENTOS] Circuito aberto — reenfileirando mensagem.");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                    return;
                }

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var tentativas = 0;

                if (ea.BasicProperties.Headers != null &&
                    ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryObj))
                {
                    tentativas = Convert.ToInt32(retryObj);
                }

                try
                {
                    var evento = JsonSerializer.Deserialize<OrderPlacedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evento is null)
                    {
                        _logger.LogWarning("[PAGAMENTOS] Evento null — descartando.");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var pagamentoRepository = scope.ServiceProvider.GetRequiredService<IPagamentoRepository>();

                    var aprovado = new Random().Next(1, 10) > 2;
                    var status = aprovado ? "Approved" : "Rejected";

                    var pagamento = new PagamentoEntity
                    {
                        Id = Guid.NewGuid(),
                        UserId = evento.UserId,
                        GameId = evento.GameId,
                        Price = evento.Price,
                        Status = status,
                        ProcessedAt = DateTime.UtcNow,
                        EventoConsumido = true,
                        EventoConsumidoEm = DateTime.UtcNow,
                        EventoPublicado = false
                    };

                    await pagamentoRepository.AdicionarAsync(pagamento);
                    await pagamentoRepository.SaveChangesAsync();

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);

                    _logger.LogInformation(
                        "[PAGAMENTOS] Pagamento {Status} salvo | UserId: {UserId} | GameId: {GameId}",
                        status, evento.UserId, evento.GameId);
                }
                catch (BrokenCircuitException ex)
                {
                    _logger.LogError(ex, "[PAGAMENTOS] Circuit breaker aberto — enviando para DLQ.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
                catch (Exception ex)
                {
                    tentativas++;

                    if (tentativas >= 3)
                    {
                        _logger.LogError(ex, "[PAGAMENTOS] Máximo de tentativas — enviando para DLQ.");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    _logger.LogWarning("[PAGAMENTOS] Tentativa {N} falhou — reenfileirando.", tentativas);

                    var props = new BasicProperties
                    {
                        Persistent = true,
                        Headers = new Dictionary<string, object?>
                        {
                            { "x-retry-count", tentativas }
                        }
                    };

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativas)));
                    await _channel.BasicPublishAsync("", "order.placed", false, props, ea.Body);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            await _channel.BasicConsumeAsync("order.placed", autoAck: false, consumer: consumer);
            _logger.LogInformation("[PAGAMENTOS] Aguardando mensagens na fila order.placed...");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _channel?.DisposeAsync().GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}