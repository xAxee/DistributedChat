---
title: DistributedChat Knowledge Base
type: index
tags:
  - distributed-chat
  - obsidian
  - index
---

# DistributedChat Knowledge Base

To jest lokalny vault Obsidian dla projektu **DistributedChat**. Zawiera skrócony opis architektury, stacku, przepływów realtime, persistence, lokalnego developmentu, testów i ograniczeń.

## Mapa notatek

- [[01-Project-Overview]] — kontekst projektu, zakres MVP i główne funkcje.
- [[02-Architecture]] — warstwy, kierunki zależności i granice modułów.
- [[03-Backend]] — API, aplikacja, domena, infrastruktura i reguły backendu.
- [[04-Frontend]] — Angular SPA, struktura frontendu i integracja z API.
- [[05-Realtime-And-Messaging]] — SignalR, RabbitMQ i dystrybucja zdarzeń między instancjami.
- [[06-Database-And-Persistence]] — PostgreSQL, EF Core, migracje i repozytoria.
- [[07-Local-Development]] — uruchamianie lokalne, porty i typowe komendy.
- [[08-Testing-And-CI]] — testy, lint/format i GitHub Actions.
- [[09-Operational-Notes]] — health checki, status, konfiguracja i obserwowalność.
- [[10-Known-Limitations]] — świadome ograniczenia MVP.
- [[11-Publication-Audit]] — przegląd przed publikacją, AI smells i lista priorytetów.
- [[12-Private-Rooms-And-Management]] — prywatność pokojów, uprawnienia właściciela i linki zaproszeniowe.

## Najkrótszy opis

**DistributedChat** to MVP realtime chatu uruchamianego lokalnie w kontenerach. Stack obejmuje .NET / ASP.NET Core, SignalR, PostgreSQL, RabbitMQ, Angular, Nginx, Docker Compose i GitHub Actions.

Backend jest podzielony podobnie do Clean Architecture:

```text
backend/src/
  DistributedChat.Domain/          # encje i reguły domenowe
  DistributedChat.Application/     # use case'y, DTO, porty, walidacja
  DistributedChat.Infrastructure/  # EF Core, PostgreSQL, RabbitMQ, implementacje portów
  DistributedChat.Api/             # HTTP API, SignalR, auth, middleware, health checks
```

Frontend znajduje się w `frontend/` i jest aplikacją Angular SPA.

## Szybkie komendy

```powershell
docker compose config
docker compose build
docker compose up -d
docker compose ps
```

```powershell
dotnet restore
dotnet build DistributedChat.slnx
dotnet test DistributedChat.slnx
dotnet format DistributedChat.slnx --verify-no-changes
```

```powershell
cd frontend
npm ci
npm run lint
npm test
npm run build
```

## Główne adresy lokalne

- Aplikacja przez reverse proxy: `http://localhost/`
- Status API przez reverse proxy: `http://localhost/api/status`
- RabbitMQ Management UI: `http://localhost:15672/`

## Powiązane pliki źródłowe

- `README.md`
- `docker-compose.yml`
- `deploy/nginx/default.conf`
- `backend/src/DistributedChat.Api/Program.cs`
- `backend/src/DistributedChat.Infrastructure/DependencyInjection.cs`
- `frontend/package.json`
- `.github/workflows/pull-request-ci.yml`
- `.github/workflows/main-ci-cd.yml`
