using FCG.Pagamentos.Application.Interfaces;
using FCG.Pagamentos.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Pagamentos.Application.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IPagamentoService, PagamentoService>();
            return services;
        }
    }
}