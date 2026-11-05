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

| Variável | Obrigatória | Descrição | Padrão |
|----------|-------------|-----------|--------|
| `ConnectionStrings__DefaultConnection` | **sim** | Connection string do SQL Server (banco `FCG_Pagamentos`) | - |
| `RabbitMQ__Usuario` | **sim** | Usuário do RabbitMQ | - |
| `RabbitMQ__Senha` | **sim** | Senha do RabbitMQ | - |
| `RabbitMQ__Host` | não | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Port` | não | Porta do RabbitMQ | `5672` |
| `MongoDB__ConnectionString` | não | Connection string do MongoDB (se contiver credenciais, trate como segredo) | `mongodb://localhost:27017` |
| `MongoDB__EventLogDatabase` | não | Banco do log de eventos no MongoDB | `FCG_EventLog` |
| `NEW_RELIC_LICENSE_KEY` | em produção | Chave de licença da New Relic | - |

Não há credenciais no código nem nos `appsettings.json`. Se uma variável obrigatória faltar, o worker não inicia e informa qual chave está faltando. Em produção (AKS) os valores vêm do Azure Key Vault, injetados como variáveis de ambiente.

## Como rodar localmente

### 1. Infraestrutura (SQL Server, RabbitMQ e MongoDB)

```bash
cp .env.example .env     # edite o .env com os seus valores (o .env não é versionado)
docker compose up -d
```

Este compose usa portas deslocadas para conviver com os outros: SQL Server **1435**, RabbitMQ **5674** (management em 15674) e MongoDB **27019**.

### 2. Segredos (dotnet user-secrets)

Os valores abaixo são **exemplos falsos**. Use o `SA_PASSWORD` e o usuário/senha do RabbitMQ que você definiu no `.env`. As chaves `RabbitMQ:Port` e `MongoDB:ConnectionString` não são segredos, mas precisam apontar para as portas do compose.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1435;Database=FCG_Pagamentos;User Id=sa;Password=<SA_PASSWORD do .env>;TrustServerCertificate=True;" --project FCG.Pagamentos.Worker
dotnet user-secrets set "RabbitMQ:Port" "5674" --project FCG.Pagamentos.Worker
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb://localhost:27019" --project FCG.Pagamentos.Worker
dotnet user-secrets set "RabbitMQ:Usuario" "troque_este_usuario" --project FCG.Pagamentos.Worker
dotnet user-secrets set "RabbitMQ:Senha" "TroqueEstaSenha#2" --project FCG.Pagamentos.Worker
```

### 3. Executar

Ao iniciar, o worker aplica as migrations do EF Core automaticamente (o banco `FCG_Pagamentos` é criado se não existir).

```bash
dotnet run --project FCG.Pagamentos.Worker
```

> ⚠️ Este repositório não contém credenciais. Os exemplos acima usam valores falsos; nunca versione o `.env` nem os seus segredos reais.