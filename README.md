# FCG Pagamentos

Microsserviço responsável por processar pagamentos da plataforma FIAP Cloud Games.

## Responsabilidades

- Consumir pedidos de compra e simular processamento de pagamento
- Publicar resultado do pagamento via Outbox Pattern

## Eventos consumidos

| Evento | Fila | Ação |
|--------|------|------|
| `OrderPlacedEvent` | `order.placed` | Processa pagamento e salva resultado |

## Eventos publicados

| Evento | Fila | Quando |
|--------|------|--------|
| `PaymentProcessedEvent` | `payment.processed` | Após processar pagamento (Approved/Rejected) |
| `PaymentProcessedEvent` | `payment.processed.notificacoes` | Após processar pagamento (Approved/Rejected) |

## Variáveis de ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ConnectionStrings__DefaultConnection` | Connection string do SQL Server | - |
| `RabbitMQ__Host` | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Port` | Porta do RabbitMQ | `5672` |
| `RabbitMQ__Usuario` | Usuário do RabbitMQ | `guest` |
| `RabbitMQ__Senha` | Senha do RabbitMQ | `guest` |

## Como rodar localmente

```bash
dotnet run --project FCG.Pagamentos.Worker
```