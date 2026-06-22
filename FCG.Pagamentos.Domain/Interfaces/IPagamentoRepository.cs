using FCG.Pagamentos.Domain.Entities;

namespace FCG.Pagamentos.Domain.Interfaces
{
    public interface IPagamentoRepository
    {
        Task AdicionarAsync(PagamentoEntity pagamento);
        Task<List<PagamentoEntity>> ObterNaoPublicadosAsync();
        Task AtualizarAsync(PagamentoEntity pagamento);
        Task<int> SaveChangesAsync();
    }
}