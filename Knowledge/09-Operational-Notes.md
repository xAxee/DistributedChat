---
title: Operational Notes
type: operations
tags:
  - distributed-chat
  - health-checks
  - operations
---

# Operational Notes

[[00-Start|Powrót do indeksu]]

## Reverse proxy

W Docker Compose przed API stoi Nginx. Publicznie wystawia:

- SPA pod `/`,
- REST API pod `/api`,
- SignalR hub pod `/hubs/chat`.

Ważny plik:

- `deploy/nginx/default.conf`

## Instancje API

Compose uruchamia dwie instancje backendu:

- `chat-service-1`
- `chat-service-2`

Celem jest sprawdzenie scenariusza rozproszonego bez sticky sessions, gdzie wiadomości są synchronizowane przez RabbitMQ.

## Health checki

API ma trzy poziomy statusu:

- `GET /health/live` — liveness procesu API,
- `GET /health/ready` — readiness z PostgreSQL i RabbitMQ,
- `GET /api/status` — status aplikacyjny.

Nginx ma własne:

- `GET /health`

Kontener frontendu ma analogiczny endpoint health w swoim Nginx.

## `GET /api/status`

Status aplikacyjny obejmuje m.in.:

- `instanceId`,
- aktywne połączenia,
- liczbę użytkowników,
- uptime,
- czas startu,
- wersję.

## Logowanie

Backend używa Serilog. Przy zmianach logowania:

- loguj korelację requestów, jeśli jest dostępna,
- nie loguj sekretów, haseł, JWT ani connection stringów,
- dla błędów biznesowych preferuj czytelne Problem Details zamiast surowych wyjątków.

## Konfiguracja

Ważne sekcje/zmienne:

- `ConnectionStrings__DistributedChat`
- `Jwt__SigningKey`
- `RabbitMq__HostName`
- `RabbitMq__Port`
- `RabbitMq__UserName`
- `RabbitMq__Password`
- `RabbitMq__VirtualHost`
- `RabbitMq__ExchangeName`
- `Messaging__Transport`
- `Instance__InstanceId`

## Bezpieczeństwo operacyjne

- Domyślne sekrety w Compose są tylko lokalne.
- Nie commituj realnych sekretów.
- Jeśli dodajesz nowe sekrety, zaktualizuj `.env.example` i dokumentację.
- JWT signing key musi być mocny poza środowiskiem lokalnym.

## Powiązane notatki

- [[05-Realtime-And-Messaging]]
- [[07-Local-Development]]
- [[10-Known-Limitations]]
