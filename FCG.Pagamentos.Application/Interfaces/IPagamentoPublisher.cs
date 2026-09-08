using FCG.Pagamentos.Application.Events;

namespace FCG.Pagamentos.Application.Interfaces
{
    public interface IPagamentoService
    {
        Task ProcessarAsync(OrderPlacedEvent evento, CancellationToken cancellationToken = default);
    }
}