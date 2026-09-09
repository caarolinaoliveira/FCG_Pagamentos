namespace FCG.Pagamentos.Application.Interfaces
{
    public interface IErroLogService
    {
        Task RegistrarAsync(Exception excecao, string contexto, object? dadosAdicionais = null);
    }
}
