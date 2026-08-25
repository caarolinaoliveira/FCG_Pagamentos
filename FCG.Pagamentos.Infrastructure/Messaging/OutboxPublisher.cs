using FCG.Pagamentos.Application.Events;
using FCG.Pagamentos.Application.Interfaces;
using FCG.Pagamentos.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FCG.Pagamentos.Infrastructure.Messaging
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisher> _logger;

        public OutboxPublisher(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxPublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
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
                        try
                        {
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
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[OUTBOX] Erro geral no publisher.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}