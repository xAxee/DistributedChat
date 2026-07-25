---
title: Frontend
type: frontend
tags:
  - distributed-chat
  - frontend
  - angular
---

# Frontend

[[00-Start|Powrót do indeksu]]

## Lokalizacja

Frontend znajduje się w:

```text
frontend/
```

Jest to Angular SPA budowane i serwowane w kontenerze Nginx.

## Stack frontendu

- Angular
- TypeScript
- RxJS
- `@microsoft/signalr`
- PrimeNG / PrimeIcons / PrimeUIX themes
- Vitest
- Angular ESLint
- Prettier

## Struktura aplikacji

Najważniejsze katalogi:

```text
frontend/backend/src/app/
  core/
    api/
    auth/
    chat/
    models/
    rooms/
  features/
    auth/
    chat/
    rooms/
  home/
```

## Obszary odpowiedzialności

### `core/auth`

Odpowiada za:

- logowanie/rejestrację,
- przechowywanie tokenu,
- guard routingu,
- interceptor JWT.

### `core/chat`

Odpowiada za:

- połączenie z SignalR,
- wysyłanie i odbiór eventów,
- deduplikację wiadomości.

### `core/rooms`

Odpowiada za komunikację HTTP z endpointami pokojów.

### `core/api`

Odpowiada za wspólne mapowanie błędów oraz odczyt danych operacyjnych:

- `GET /api/status`,
- `GET /health/live`,
- `GET /health/ready`.

Dashboard pokojów prezentuje liczbę aktywnych użytkowników i połączeń,
uptime, wersję/instancję API oraz wynik health checków. Ścieżki `/health/*`
są przekazywane do backendu przez publiczny reverse proxy Nginx; `/health`
bez sufiksu pozostaje health checkiem samego proxy.

### `features/*`

Komponenty stron i ich style:

- login,
- register,
- lista pokojów,
- chat w pokoju.

## Komendy

```powershell
cd frontend
npm ci
npm run lint
npm test
npm run build
npm run format:check
```

## Integracja z backendem

Przez Nginx frontend korzysta z publicznych ścieżek:

- REST API: `/api/...`
- SignalR hub: `/hubs/chat`
- health backendu: `/health/live` i `/health/ready`

Token JWT powinien trafiać:

- do nagłówka `Authorization: Bearer <token>` dla HTTP,
- jako `access_token` dla połączeń SignalR WebSocket.

## Zasady zmian frontendowych

- Utrzymuj typy DTO zgodne z kontraktem API.
- Nie duplikuj logiki auth w komponentach — używaj serwisów z `core/auth`.
- Nie twórz bezpośrednich wywołań `fetch` w komponentach, jeśli istnieje serwis API.
- Po zmianach w API sprawdź modele w `core/models/`.
- Po zmianach uruchom lint, test i build.

## Powiązane notatki

- [[03-Backend]]
- [[05-Realtime-And-Messaging]]
- [[07-Local-Development]]
