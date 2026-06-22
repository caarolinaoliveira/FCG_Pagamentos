using FCG.Pagamentos.Domain.Entities;
using FCG.Pagamentos.Domain.Interfaces;
using FCG.Pagamentos.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Pagamentos.Infrastructure.Repository
{
    public class PagamentoRepository : IPagamentoRepository
    {
        private readonly PagamentosDbContext _context;

        public PagamentoRepository(PagamentosDbContext context)
        {
            _context = context;
        }

        public async Task AdicionarAsync(PagamentoEntity pagamento)
        {
            await _context.Pagamentos.AddAsync(pagamento);
        }

        public async Task<List<PagamentoEntity>> ObterNaoPublicadosAsync()
        {
            return await _context.Pagamentos
                .AsNoTracking()
                .Where(p => !p.EventoPublicado)
                .ToListAsync();
        }

        public async Task AtualizarAsync(PagamentoEntity pagamento)
        {
            _context.Pagamentos.Update(pagamento);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}