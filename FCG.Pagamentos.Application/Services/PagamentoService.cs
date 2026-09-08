using FCG.Pagamentos.Application.Events;
using FCG.Pagamentos.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FCG.Pagamentos.Application.Services
{
    public class PagamentoService : IPagamentoService
    {
        private const string TopicoPagamentoProcessado = "payment.processed";
        private const int ProbabilidadeAprovacaoPercentual = 80; // 8 em 10

        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<PagamentoService> _logger;
        private readonly Random _random = Random.Shared;

        public PagamentoService(
            IMessagePublisher messagePublisher,
            ILogger<PagamentoService> logger)
        {
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task ProcessarAsync(OrderPlacedEvent evento, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(evento);

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["UserId"] = evento.UserId,
                ["GameId"] = evento.GameId
            });

            _logger.LogInformation(
                "Processando pagamento no valor de {Price:C} para o jogo {GameId}",
                evento.Price, evento.GameId);

            var aprovado = _random.Next(1, 101) <= ProbabilidadeAprovacaoPercentual;
            var status = aprovado ? PaymentStatus.Approved : PaymentStatus.Rejected;

            var resultado = new PaymentProcessedEvent
            {
                UserId = evento.UserId,
                GameId = evento.GameId,
                Price = evento.Price,
                Status = status.ToString(),
                ProcessedAt = DateTime.UtcNow
            };

            try
            {
                await _messagePublisher.PublicarAsync(resultado, TopicoPagamentoProcessado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao publicar resultado de pagamento {Status} para o jogo {GameId}",
                    status, evento.GameId);
                throw;
            }

            if (aprovado)
            {
                _logger.LogInformation("Pagamento aprovado para o jogo {GameId}", evento.GameId);
            }
            else
            {
                _logger.LogWarning("Pagamento rejeitado para o jogo {GameId}", evento.GameId);
            }
        }
    }

    public enum PaymentStatus
    {
        Approved,
        Rejected
    }
}