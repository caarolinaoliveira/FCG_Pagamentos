using FCG.Pagamentos.Application.Events;
using FCG.Pagamentos.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FCG.Pagamentos.Application.Services
{
    public class PagamentoService : IPagamentoService
    {
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<PagamentoService> _logger;

        public PagamentoService(
            IMessagePublisher messagePublisher,
            ILogger<PagamentoService> logger)
        {
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task ProcessarAsync(OrderPlacedEvent evento)
        {
            _logger.LogInformation(
                "[PAGAMENTOS] Processando pagamento | UserId: {UserId} | GameId: {GameId} | Price: {Price}",
                evento.UserId, evento.GameId, evento.Price);

            var aprovado = new Random().Next(1, 10) > 2;
            var status = aprovado ? "Approved" : "Rejected";

            var resultado = new PaymentProcessedEvent
            {
                UserId = evento.UserId,
                GameId = evento.GameId,
                Price = evento.Price,
                Status = status,
                ProcessedAt = DateTime.UtcNow
            };

            await _messagePublisher.PublicarAsync(resultado, "payment.processed");

            _logger.LogInformation(
                "[PAGAMENTOS] Pagamento {Status} | UserId: {UserId} | GameId: {GameId}",
                status, evento.UserId, evento.GameId);
        }
    }
}