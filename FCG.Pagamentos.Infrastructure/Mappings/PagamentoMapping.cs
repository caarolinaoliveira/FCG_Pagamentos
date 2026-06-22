using FCG.Pagamentos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Pagamentos.Infrastructure.Mappings
{
    public class PagamentoMapping : IEntityTypeConfiguration<PagamentoEntity>
    {
        public void Configure(EntityTypeBuilder<PagamentoEntity> builder)
        {
            builder.ToTable("Pagamentos");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.UserId)
                .IsRequired();

            builder.Property(p => p.GameId)
                .IsRequired();

            builder.Property(p => p.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(p => p.ProcessedAt)
                .IsRequired();

            builder.Property(p => p.EventoConsumido)
                .IsRequired();

            builder.Property(p => p.EventoConsumidoEm);

            builder.Property(p => p.EventoPublicado)
                .IsRequired();

            builder.Property(p => p.EventoPublicadoEm);
        }
    }
}