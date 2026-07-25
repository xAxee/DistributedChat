---
title: Database And Persistence
type: persistence
tags:
  - distributed-chat
  - postgres
  - ef-core
---

# Database And Persistence

[[00-Start|Powrót do indeksu]]

## Storage

Główną bazą danych jest **PostgreSQL**. Backend używa **Entity Framework Core** z providerem Npgsql.

## Główne encje

Encje domenowe znajdują się w `backend/src/DistributedChat.Domain/`:

- `Users/User.cs`
- `Users/UserPresence.cs`
- `Rooms/Room.cs`
- `Rooms/RoomMember.cs`
- `Messages/Message.cs`
- `ProcessedEvents/ProcessedEvent.cs`

## DbContext i konfiguracje

Ważne pliki infrastruktury:

- `backend/src/DistributedChat.Infrastructure/Persistence/DistributedChatDbContext.cs`
- `backend/src/DistributedChat.Infrastructure/Persistence/DistributedChatDbContextFactory.cs`
- `backend/src/DistributedChat.Infrastructure/Persistence/Configurations/*Configuration.cs`
- `backend/src/DistributedChat.Infrastructure/Persistence/Migrations/*`

## Store / repozytoria

Implementacje portów aplikacyjnych:

- `Persistence/Users/UserAccountStore.cs`
- `Persistence/Users/UserPresenceStore.cs`
- `Persistence/Rooms/RoomStore.cs`
- `Persistence/Messages/MessageStore.cs`

Porty są definiowane w `Application`, np.:

- `IUserAccountStore`
- `IUserPresenceStore`
- `IRoomStore`
- `IMessageStore`

## Connection string

Nazwa connection stringa:

```text
DistributedChat
```

W środowisku można ustawić przez:

```text
ConnectionStrings__DistributedChat
```

## Migracje

Migracje są w projekcie `DistributedChat.Infrastructure`.

Przykładowe komendy EF Core należy uruchamiać świadomie i dopasować do aktualnej wersji SDK/projektu. Przy zmianie modelu:

1. Zmień encję domenową.
2. Zmień konfigurację EF Core.
3. Dodaj lub popraw store.
4. Dodaj migrację.
5. Dodaj testy integracyjne persistence.

## Zasady zmian persistence

- `Application` nie powinno zależeć od EF Core.
- Zapytania specyficzne dla bazy trzymaj w `Infrastructure`.
- Unikaj ładowania nadmiarowych danych w hot path wiadomości.
- Zmiany schematu powinny mieć migrację.
- Testy integracyjne z bazą powinny używać istniejących wzorców Testcontainers.

## Powiązane notatki

- [[02-Architecture]]
- [[03-Backend]]
- [[05-Realtime-And-Messaging]]
- [[08-Testing-And-CI]]
