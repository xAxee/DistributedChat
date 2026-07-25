# DistributedChat

DistributedChat to niewielki projekt, który zrobiłem głównie po to, żeby w praktyce
sprawdzić współpracę **RabbitMQ** i **SignalR** w aplikacji uruchomionej na kilku
instancjach backendu.

Użytkownik łączy się z jedną z dwóch instancji API przez Nginx. Wiadomości trafiają
do PostgreSQL, a zdarzenia są publikowane do RabbitMQ. Każda instancja odbiera je
i przekazuje swoim klientom przez SignalR. Dzięki temu osoby podłączone do różnych
instancji nadal widzą ten sam czat.

## Co jest w projekcie
- Rejestracja i logowanie z JWT,
- Pokoje oraz historia wiadomości,
- Realtime przez SignalR,
- Wymiana zdarzeń między instancjami przez RabbitMQ,
- Backend w .NET 10 i frontend w Angularze,
- PostgreSQL, Nginx i Docker Compose,
- testy oraz workflow GitHub Actions.

## Uruchomienie
Projekt wymaga Dockera. Skopiuj przykładową konfigurację i uruchom cały stack:

```
docker compose up -d --build
```

Aplikacja będzie dostępna pod `http://localhost`,
panel RabbitMQ pod `http://localhost:15672`

* Domyślne dane w `.env.example` służą wyłącznie do uruchamiania lokalnego.

## Struktura
```
backend/    API, logika aplikacji, infrastruktura i testy
frontend/   aplikacja Angular
deploy/     konfiguracja Nginx
```
