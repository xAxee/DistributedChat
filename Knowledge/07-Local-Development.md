---
title: Local Development
type: operations
tags:
  - distributed-chat
  - docker-compose
  - local-development
---

# Local Development

[[00-Start|Powrót do indeksu]]

## Uruchomienie pełnego stacku

```powershell
docker compose config
docker compose build
docker compose up -d
docker compose ps
```

Zatrzymanie:

```powershell
docker compose down
```

## Adresy lokalne

| Usługa | Adres |
| --- | --- |
| Aplikacja | `http://localhost/` |
| Status API | `http://localhost/api/status` |
| RabbitMQ Management | `http://localhost:15672/` |
| Health Nginx | `http://localhost/health` |

## Domyślne porty

| Usługa | Port hosta | Port w kontenerze |
| --- | ---: | ---: |
| `nginx` | `80` | `80` |
| `chat-service-1` | `5081` | `8080` |
| `chat-service-2` | `5082` | `8080` |
| `postgres` | `5432` | `5432` |
| `rabbitmq` | `5672` | `5672` |
| `rabbitmq management` | `15672` | `15672` |

## Konfiguracja lokalna

W większości przypadków `.env` nie jest wymagany, bo `docker-compose.yml` ma wartości domyślne.

Najczęściej nadpisywane zmienne:

- `POSTGRES_PORT`
- `RABBITMQ_PORT`
- `RABBITMQ_MANAGEMENT_PORT`
- `CHAT_SERVICE_1_PORT`
- `CHAT_SERVICE_2_PORT`
- `NGINX_HTTP_PORT`
- `JWT_SIGNING_KEY`

## Backend lokalnie

```powershell
dotnet restore
dotnet build DistributedChat.slnx
dotnet test DistributedChat.slnx
dotnet format DistributedChat.slnx --verify-no-changes
```

## Frontend lokalnie

```powershell
cd frontend
npm ci
npm run lint
npm test
npm run build
```

## Powiązane notatki

- [[08-Testing-And-CI]]
- [[09-Operational-Notes]]
