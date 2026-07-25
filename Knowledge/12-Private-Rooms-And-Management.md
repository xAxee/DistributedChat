---
title: Private Rooms And Management
type: feature
tags:
  - distributed-chat
  - rooms
  - authorization
  - invitations
---

# Private Rooms And Management

[[00-Start|Powrót do indeksu]]

## Widoczność pokojów

Podczas tworzenia użytkownik wybiera pokój publiczny albo prywatny.

- Publiczne pokoje są widoczne w katalogu i można do nich dołączyć bez hasła.
- Prywatne pokoje są widoczne na liście tylko dla ich członków.
- Zwykłe dołączenie do prywatnego pokoju wymaga hasła.
- Właściciel jest automatycznie pierwszym członkiem i nie może opuścić ani usunąć samego siebie.

Hasła pokojów korzystają z `IPasswordHasher` i w bazie są przechowywane wyłącznie
jako hash. API nigdy nie zwraca hasła ani jego hasha.

## Uprawnienia właściciela

Tylko `CreatedByUserId` może:

- zmienić nazwę pokoju,
- zmienić hasło prywatnego pokoju,
- usunąć innego członka,
- usunąć pokój wraz z wiadomościami i członkostwami,
- wygenerować lub odnowić link zaproszeniowy.

Backend sprawdza własność w `RoomService`; frontendowe ukrycie kontrolek nie jest
granicą bezpieczeństwa.

## Linki zaproszeniowe

`POST /api/rooms/{roomId}/invite` generuje 256-bitowy losowy token. API zwraca
surowy token tylko w tej odpowiedzi, a PostgreSQL przechowuje jego hash SHA-256.
Frontend buduje link `/invite/{token}`.

`POST /api/rooms/invitations/{token}/join` dodaje uwierzytelnionego użytkownika
bez podawania hasła pokoju. Wygenerowanie nowego linku zastępuje hash, więc
poprzedni link natychmiast przestaje działać.

## Realtime po utracie członkostwa

SignalR nie jest źródłem prawdy dla autoryzacji. Przed wysłaniem wiadomości lub
zdarzenia obecności `SignalRChatClientNotifier` pobiera aktualne identyfikatory
członków z PostgreSQL i wysyła zdarzenie tylko do ich lokalnych połączeń.
Dzięki temu wyrzucony użytkownik nie otrzymuje kolejnych zdarzeń nawet wtedy,
gdy jego techniczna subskrypcja grupy powstała na innej instancji API.

## Persistence

Migracja `AddPrivateRoomsAndManagement` dodaje do `rooms`:

- `is_private`,
- `password_hash`,
- `invite_token_hash` z unikalnym indeksem.

Relacje wiadomości i członkostw do pokoju używają kaskadowego usuwania. Relacja
właściciela pokoju do użytkownika nadal jest restrykcyjna.

## Endpointy

| Metoda | Ścieżka | Dostęp |
| --- | --- | --- |
| `POST` | `/api/rooms` | zalogowany użytkownik |
| `POST` | `/api/rooms/{roomId}/join` | hasło dla prywatnego pokoju |
| `POST` | `/api/rooms/invitations/{token}/join` | posiadacz aktywnego linku |
| `PUT` | `/api/rooms/{roomId}` | właściciel |
| `PUT` | `/api/rooms/{roomId}/password` | właściciel prywatnego pokoju |
| `DELETE` | `/api/rooms/{roomId}/members/{userId}` | właściciel |
| `POST` | `/api/rooms/{roomId}/invite` | właściciel |
| `DELETE` | `/api/rooms/{roomId}` | właściciel |
