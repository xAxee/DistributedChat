---
title: Realtime And Messaging
type: messaging
tags:
  - distributed-chat
  - signalr
  - rabbitmq
  - realtime
---

# Realtime And Messaging

[[00-Start|Powrót do indeksu]]

## Cel

Projekt uruchamia dwie instancje API za reverse proxy. SignalR domyślnie rozsyła wiadomości tylko do klientów podłączonych do danej instancji. RabbitMQ jest używany jako transport zdarzeń, żeby druga instancja także mogła powiadomić swoich lokalnych klientów.

## Przepływ wiadomości

1. Klient łączy się z `/hubs/chat` i dołącza konkretnym połączeniem do grupy pokoju przez `JoinRoom`.
2. Klient wywołuje `SendMessage` z `roomId` i treścią.
3. API waliduje uprawnienia i treść.
4. Wiadomość jest zapisywana w PostgreSQL.
5. API publikuje `ChatMessageCreated` do RabbitMQ.
6. Konsument RabbitMQ w każdej instancji odbiera zdarzenie.
7. Instancja wysyła `MessageReceived` do lokalnych klientów w grupie pokoju.
8. Mechanizm processed events ogranicza skutki duplikatów.

## SignalR

Ważne pliki:

- `backend/src/DistributedChat.Api/Hubs/ChatHub.cs`
- `backend/src/DistributedChat.Api/Hubs/ChatConnectionLifecycleService.cs`
- `backend/src/DistributedChat.Api/Hubs/ChatRoomSubscriptionService.cs`
- `backend/src/DistributedChat.Api/Hubs/ChatHubEvents.cs`
- `backend/src/DistributedChat.Api/Hubs/ChatHubGroups.cs`
- `backend/src/DistributedChat.Api/Hubs/ConnectionRegistry.cs`
- `backend/src/DistributedChat.Api/Hubs/SignalRChatClientNotifier.cs`
- `frontend/backend/src/app/core/chat/chat-realtime.service.ts`

Znane ograniczenie: brak sticky sessions w Nginx, więc klient SignalR powinien używać WebSocketów z `skipNegotiation: true`.

### Członkostwo pokoju a subskrypcja SignalR

PostgreSQL pozostaje źródłem prawdy dla trwałego członkostwa pokoju. Endpointy `POST /api/rooms/{roomId}/join` i `POST /api/rooms/{roomId}/leave` dodają lub usuwają rekord członkostwa. SignalR-owe `JoinRoom` i `LeaveRoom` nie zmieniają członkostwa; zarządzają wyłącznie tym, czy dany `ConnectionId` jest zapisany do lokalnej grupy SignalR pokoju.

Przed dodaniem lub usunięciem subskrypcji hub sprawdza, czy pokój istnieje i czy użytkownik jest członkiem pokoju. `UserJoinedRoom` oraz `UserLeftRoom` są publikowane tylko po rzeczywistej zmianie subskrypcji grupy. Rozłączenie klienta czyści lokalne subskrypcje połączenia bez publikowania `UserLeftRoom` i bez usuwania członkostwa z bazy.

Przy wysyłaniu zdarzeń do klientów notifier dodatkowo filtruje lokalne
subskrypcje według aktualnego członkostwa w PostgreSQL. Chroni to prywatne
pokoje po usunięciu członka, także gdy akcja właściciela i połączenie usuniętego
użytkownika trafiły do różnych instancji API.

## RabbitMQ

RabbitMQ używa domyślnego exchange:

```text
chat.events
```

Konfigurowane przez:

```text
RabbitMq__ExchangeName
```

Domyślne dane lokalne:

- user: `distributed_chat`
- password: `distributed_chat_local_password`
- virtual host: `distributed_chat`

Ważne pliki:

- `backend/src/DistributedChat.Application/Messages/ChatMessageCreated.cs`
- `backend/src/DistributedChat.Infrastructure/Messaging/RabbitMqChatEventPublisher.cs`
- `backend/src/DistributedChat.Infrastructure/Messaging/RabbitMqChatEventConsumer.cs`
- `backend/src/DistributedChat.Infrastructure/Messaging/ChatEventProcessor.cs`
- `backend/src/DistributedChat.Infrastructure/Messaging/RabbitMqConnection.cs`
- `backend/src/DistributedChat.Infrastructure/Messaging/RabbitMqTopology.cs`

## Zasady zmian

- Nie zmieniaj łamiąco payloadu `ChatMessageCreated`, jeśli może to przerwać komunikację między uruchomionymi instancjami API.
- Publikowanie zdarzenia powinno następować po trwałym zapisie wiadomości.
- Konsument musi być odporny na duplikaty i błędy deserializacji.
- Nie zakładaj sticky sessions.
- Uważaj na echo wiadomości i deduplikację po stronie klienta.

## Powiązane notatki

- [[03-Backend]]
- [[06-Database-And-Persistence]]
- [[09-Operational-Notes]]
