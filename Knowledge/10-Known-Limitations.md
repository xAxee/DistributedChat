---
title: Known Limitations
type: limitations
tags:
  - distributed-chat
  - limitations
  - mvp
---

# Known Limitations

[[00-Start|Powrót do indeksu]]

To jest lista świadomych ograniczeń MVP. Przy planowaniu zmian warto sprawdzać, czy dana praca usuwa jedno z nich, czy tylko je omija.

## Deployment

- Brak produkcyjnego deploymentu.
- CI kończy się na buildzie, testach i publikacji obrazów do GHCR.

## SignalR / Nginx

- Brak sticky sessions w Nginx.
- Klient SignalR musi używać WebSocketów z `skipNegotiation: true`.
- Projekt zakłada synchronizację wiadomości przez RabbitMQ zamiast affinity sesji.

## Sekrety

- Domyślne sekrety w Compose są tylko do lokalnego uruchomienia.
- Nie nadają się do środowisk produkcyjnych.

## Uwierzytelnianie

- JWT jest proste.
- Brak refresh tokenów.
- Brak rozbudowanego zarządzania sesjami.

## Autoryzacja i moderacja

- Brak ról administracyjnych.
- Brak moderacji pokojów.
- Brak zaawansowanych uprawnień per pokój.

## Rate limiting

- Rate limiting jest lokalny dla instancji API.
- Nie jest globalny dla całego klastra.

## Presence

- Obecność użytkowników jest wystarczająca dla MVP.
- Nie jest pełnym systemem presence odpornym na trudne przypadki, np. długie partycje sieci i awarie klientów.

## Frontend

- Frontend jest MVP.
- Nie obsługuje wszystkich możliwych stanów błędów, które API potrafi zwrócić.

## Potencjalne kierunki rozwoju

- Produkcyjny deployment.
- Refresh tokeny i rotacja sesji.
- Role i moderacja.
- Globalny/distributed rate limiting.
- Pełniejszy presence system.
- Lepsza obsługa błędów i retry w UI.
- Observability: metryki, tracing, dashboardy.
