---
title: Architecture
type: architecture
tags:
  - distributed-chat
  - clean-architecture
  - dotnet
---

# Architecture

[[00-Start|Powrót do indeksu]]

## Styl architektoniczny

Backend jest podzielony podobnie do **Clean Architecture**. Najważniejsza reguła: logika domenowa i aplikacyjna nie powinna zależeć od szczegółów infrastruktury ani od ASP.NET Core.

```text
DistributedChat.Api
        ↓
DistributedChat.Application  →  DistributedChat.Domain
        ↑
DistributedChat.Infrastructure
```

## Warstwy

### `backend/src/DistributedChat.Domain/`

Zawiera encje i reguły domenowe:

- `Users/User.cs`
- `Users/UserPresence.cs`
- `Rooms/Room.cs`
- `Rooms/RoomMember.cs`
- `Messages/Message.cs`
- `ProcessedEvents/ProcessedEvent.cs`

Zasada: domena powinna pozostać możliwie niezależna od frameworków.

### `backend/src/DistributedChat.Application/`

Zawiera przypadki użycia, DTO, walidację i porty:

- `Authentication/AuthService.cs`
- `Rooms/RoomService.cs`
- `Messages/MessageService.cs`
- `Users/CurrentUserService.cs`
- `Common/Abstractions/*`
- `Common/Results/*`

Zasada: ta warstwa definiuje, czego potrzebuje aplikacja, np. `IMessageStore`, `IRoomStore`, `IJwtTokenGenerator`, `IChatEventPublisher`.

### `backend/src/DistributedChat.Infrastructure/`

Zawiera implementacje portów aplikacyjnych:

- EF Core i PostgreSQL,
- RabbitMQ,
- JWT token generation,
- password hashing,
- health checks zależne od infrastruktury.

Główne miejsce rejestracji zależności: `backend/src/DistributedChat.Infrastructure/DependencyInjection.cs`.

### `backend/src/DistributedChat.Api/`

Zawiera adapter HTTP i realtime:

- Minimal API endpoints,
- SignalR hub,
- middleware,
- auth i authorization,
- rate limiting,
- Problem Details,
- health endpoints,
- composition root w `Program.cs`.

## Kierunki zależności

Preferowane kierunki:

- `Api` może znać `Application` i `Infrastructure`.
- `Infrastructure` może znać `Application` i `Domain`.
- `Application` może znać `Domain` i abstrakcje, ale nie szczegóły infrastruktury.
- `Domain` nie powinien zależeć od `Api`, `Infrastructure` ani frameworków.

## Granice zmian

Przy dodawaniu nowych funkcji:

1. Zacznij od modelu domenowego lub use case'u w `Application`.
2. Dodaj port w `Application/Common/Abstractions` lub module feature, jeśli potrzeba.
3. Dodaj implementację w `Infrastructure`.
4. Wystaw funkcję przez `Api` lub SignalR.
5. Dodaj testy unit/integration w `backend/tests/`.
6. Dopiero potem aktualizuj frontend.

## Powiązane notatki

- [[03-Backend]]
- [[05-Realtime-And-Messaging]]
- [[06-Database-And-Persistence]]
- [[08-Testing-And-CI]]
