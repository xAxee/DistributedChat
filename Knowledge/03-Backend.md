---
title: Backend
type: backend
tags:
  - distributed-chat
  - backend
  - aspnet-core
  - signalr
---

# Backend

[[00-Start|Powrót do indeksu]]

## Stack backendu

- .NET / ASP.NET Core
- SignalR
- Entity Framework Core
- PostgreSQL przez Npgsql
- RabbitMQ.Client
- FluentValidation
- Serilog
- JWT Bearer authentication
- xUnit i Testcontainers dla testów

Repo używa centralnego zarządzania wersjami pakietów w `Directory.Packages.props`.

## Composition root

Główny start aplikacji: `backend/src/DistributedChat.Api/Program.cs`.

Pipeline rejestruje m.in.:

- opcje API,
- kontekst HTTP i current user,
- kontrolery,
- health checki,
- SignalR,
- rate limiting,
- Swagger,
- Problem Details,
- JWT auth,
- warstwę `Application`,
- warstwę `Infrastructure`,
- messaging API.

## REST API

API zwraca błędy jako `application/problem+json`. Endpointy poza rejestracją, logowaniem, rootem i statusem wymagają JWT:

```http
Authorization: Bearer <token>
```

Najważniejsze endpointy:

| Metoda | Ścieżka                                         | Auth | Opis                 |
| ------ | ----------------------------------------------- | ---- | -------------------- |
| `GET`  | `/`                                             | nie  | root/status prosty   |
| `POST` | `/api/auth/register`                            | nie  | rejestracja i JWT    |
| `POST` | `/api/auth/login`                               | nie  | logowanie i JWT      |
| `GET`  | `/api/users/me`                                 | tak  | aktualny użytkownik  |
| `POST` | `/api/rooms`                                    | tak  | tworzy pokój         |
| `GET`  | `/api/rooms`                                    | tak  | lista pokojów        |
| `GET`  | `/api/rooms/{roomId}`                           | tak  | szczegóły pokoju     |
| `POST` | `/api/rooms/{roomId}/join`                      | tak  | dołączenie do pokoju |
| `POST` | `/api/rooms/{roomId}/leave`                     | tak  | opuszczenie pokoju   |
| `GET`  | `/api/rooms/{roomId}/members`                   | tak  | lista członków       |
| `GET`  | `/api/rooms/{roomId}/messages?before=&limit=50` | tak  | historia wiadomości  |
| `GET`  | `/api/status`                                   | nie  | status aplikacyjny   |

## SignalR hub

Hub znajduje się pod `/hubs/chat`.

Połączenie wymaga JWT. Dla WebSocketów token jest obsługiwany przez query string `access_token`, zgodnie ze standardowym mechanizmem klienta SignalR.

Metody klient → hub:

| Metoda        | Payload               | Opis                                                                                             |
| ------------- | --------------------- | ------------------------------------------------------------------------------------------------ |
| `JoinRoom`    | `roomId: Guid`        | po sprawdzeniu członkostwa dodaje konkretne połączenie do grupy pokoju                           |
| `LeaveRoom`   | `roomId: Guid`        | po sprawdzeniu członkostwa usuwa konkretne połączenie z grupy pokoju, jeśli było zasubskrybowane |
| `SendMessage` | `{ roomId, content }` | zapisuje i publikuje wiadomość                                                                   |

REST-owe członkostwo pokoju i SignalR-owa subskrypcja grupy to osobne pojęcia. `POST /api/rooms/{roomId}/join` oraz `POST /api/rooms/{roomId}/leave` zmieniają trwałe członkostwo w PostgreSQL. `JoinRoom` i `LeaveRoom` w hubie zmieniają tylko techniczną subskrypcję danego `ConnectionId` do grupy SignalR i wymagają, żeby użytkownik był członkiem pokoju. Rozłączenie SignalR usuwa techniczne subskrypcje połączenia, ale nie usuwa członkostwa w pokoju.

Zdarzenia hub → klient:

| Zdarzenie         | Kiedy                                                                  |
| ----------------- | ---------------------------------------------------------------------- |
| `MessageReceived` | po zapisaniu wiadomości w pokoju                                       |
| `UserJoinedRoom`  | gdy połączenie użytkownika zostanie dodane do grupy SignalR pokoju     |
| `UserLeftRoom`    | gdy zasubskrybowane połączenie użytkownika opuści grupę SignalR pokoju |

## Styl kodu backendu

Konfiguracja w `Directory.Build.props`:

- `Nullable` włączone,
- implicit usings włączone,
- analyzery .NET włączone,
- warnings as errors,
- dokumentacja XML generowana,
- `LangVersion` ustawiony na `14.0`.

Przy zmianach backendu pilnuj:

- nie obchodź `Result` / `ApplicationError` tam, gdzie istnieje wzorzec wyników,
- walidację wejścia dodawaj przez FluentValidation,
- nie wkładaj EF Core do `Application` ani `Domain`,
- nie loguj sekretów, tokenów ani haseł,
- dla nowych endpointów utrzymuj Problem Details i sensowne statusy HTTP.

## Powiązane notatki

- [[02-Architecture]]
- [[05-Realtime-And-Messaging]]
- [[06-Database-And-Persistence]]
- [[08-Testing-And-CI]]
