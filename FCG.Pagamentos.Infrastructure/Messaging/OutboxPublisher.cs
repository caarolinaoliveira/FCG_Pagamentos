using System.Diagnostics;
using FCG.Pagamentos.Application.Events;
using FCG.Pagamentos.Application.Interfaces;
using FCG.Pagamentos.Domain.Entities;
using FCG.Pagamentos.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;

namespace FCG.Pagamentos.Infrastructure.Messaging
{
    public class OutboxPublisher : BackgroundService
    {
        private static readonly ActivitySource ActivitySource = new("FCG.Pagamentos.OutboxPublisher");

        static OutboxPublisher()
        {
            ActivitySource.AddActivityListener(new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            });
        }

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisher> _logger;
        private readonly IErroLogService _erroLogService;

        public OutboxPublisher(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxPublisher> logger,
            IErroLogService erroLogService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _erroLogService = erroLogService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[OUTBOX] Iniciando publisher...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var pagamentoRepository = scope.ServiceProvider.GetRequiredService<IPagamentoRepository>();
                    var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
                    var sqsPublisher = scope.ServiceProvider.GetRequiredService<ISqsPublisher>();

                    var pendentes = await pagamentoRepository.ObterNaoPublicadosAsync();

                    foreach (var pagamento in pendentes)
                    {
                        await PublicarPagamentoAsync(pagamento, pagamentoRepository, messagePublisher, sqsPublisher);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[OUTBOX] Erro geral no publisher.");
                    await _erroLogService.RegistrarAsync(ex, "OutboxPublisher.ExecuteAsync");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        [Transaction(Web = false)]
        private async Task PublicarPagamentoAsync(
            PagamentoEntity pagamento,
            IPagamentoRepository pagamentoRepository,
            IMessagePublisher messagePublisher,
            ISqsPublisher sqsPublisher)
        {
            try
            {
                if (!string.IsNullOrEmpty(pagamento.TraceParent))
                {
                    NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AcceptDistributedTraceHeaders(
                        new Dictionary<string, object?> { { "traceparent", pagamento.TraceParent } },
                        (headers, chave) => headers.TryGetValue(chave, out var valor) && valor is string s
                            ? new[] { s }
                            : Enumerable.Empty<string>(),
                        TransportType.Other);
                }

                var parentContext = ExtrairContextoDistribuido(pagamento.TraceParent);
                using var activity = ActivitySource.StartActivity("outbox.publish", ActivityKind.Producer, parentContext);

                var evento = new PaymentProcessedEvent
                {
                    UserId = pagamento.UserId,
                    GameId = pagamento.GameId,
                    Price = pagamento.Price,
                    Status = pagamento.Status,
                    ProcessedAt = pagamento.ProcessedAt
                };

                await messagePublisher.PublicarAsync(evento, "payment.processed");
                await messagePublisher.PublicarAsync(evento, "payment.processed.notificacoes");
                await sqsPublisher.PublicarAsync(evento, "payment-processed-notificacoes");

                pagamento.EventoPublicado = true;
                pagamento.EventoPublicadoEm = DateTime.UtcNow;
                await pagamentoRepository.AtualizarAsync(pagamento);
                await pagamentoRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "[OUTBOX] PaymentProcessedEvent publicado | UserId: {UserId} | GameId: {GameId} | Status: {Status}",
                    pagamento.UserId, pagamento.GameId, pagamento.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OUTBOX] Erro ao publicar evento | PagamentoId: {Id}", pagamento.Id);
                await _erroLogService.RegistrarAsync(ex, "OutboxPublisher.PublicarPagamento",
                    new { PagamentoId = pagamento.Id, pagamento.UserId, pagamento.GameId });
            }
        }

        private static ActivityContext ExtrairContextoDistribuido(string? traceParent)
        {
            if (string.IsNullOrEmpty(traceParent))
            {
                return default;
            }

            return ActivityContext.TryParse(traceParent, traceState: null, out var context) ? context : default;
        }
    }
}