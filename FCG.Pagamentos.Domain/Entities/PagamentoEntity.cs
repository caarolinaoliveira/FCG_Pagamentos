namespace FCG.Pagamentos.Domain.Entities
{
    public class PagamentoEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }

        //outbox pattern
        public bool EventoConsumido { get; set; }
        public DateTime? EventoConsumidoEm { get; set; }
        public bool EventoPublicado { get; set; }
        public DateTime? EventoPublicadoEm { get; set; }
    }
}