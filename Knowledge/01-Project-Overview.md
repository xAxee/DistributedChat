---
title: Project Overview
type: overview
tags:
  - distributed-chat
  - mvp
  - overview
---

# Project Overview

[[00-Start|Powrót do indeksu]]

## Czym jest DistributedChat?

**DistributedChat** to mały, kompletny MVP realtime chatu. Projekt pokazuje aplikację, którą można uruchomić lokalnie w kilku kontenerach, z dwoma instancjami backendu za Nginx i przekazywaniem wiadomości między instancjami przez RabbitMQ.

## Zakres MVP

Projekt obejmuje:

- rejestrację i logowanie użytkowników,
- JWT authentication,
- pokoje czatu,
- dołączanie i opuszczanie pokojów,
- historię wiadomości stronicowaną kursorem,
- realtime przez SignalR,
- dystrybucję zdarzeń przez RabbitMQ,
- PostgreSQL jako storage,
- health checki i status aplikacyjny,
- Angular SPA jako frontend,
- lokalny runtime przez Docker Compose,
- CI dla backendu, frontendu i obrazów Docker.

## Czego projekt nie próbuje jeszcze rozwiązać?

To nie jest produkcyjny system społecznościowy. Świadome ograniczenia opisuje [[10-Known-Limitations]]. Najważniejsze z nich:

- brak produkcyjnego deploymentu,
- proste JWT bez refresh tokenów,
- brak moderacji, ról i uprawnień administracyjnych,
- lokalny rate limiting per instancja,
- brak pełnego systemu presence dla trudnych awarii sieciowych.

## Główne przepływy użytkownika

1. Użytkownik rejestruje konto przez `POST /api/auth/register`.
2. Użytkownik loguje się przez `POST /api/auth/login` i dostaje token JWT.
3. Frontend zapisuje token i dodaje go do żądań HTTP.
4. Użytkownik tworzy lub wybiera pokój.
5. Frontend otwiera połączenie SignalR do `/hubs/chat` z tokenem w `access_token`.
6. Klient wywołuje `JoinRoom`.
7. Wiadomość jest wysyłana przez `SendMessage`, zapisywana w PostgreSQL i publikowana jako zdarzenie.
8. Lokalna instancja i pozostałe instancje API rozsyłają zdarzenie do swoich klientów.

## Główne moduły

- [[02-Architecture]] — opis warstw i kierunków zależności.
- [[03-Backend]] — szczegóły .NET API.
- [[04-Frontend]] — szczegóły Angular SPA.
- [[05-Realtime-And-Messaging]] — SignalR i RabbitMQ.
- [[06-Database-And-Persistence]] — EF Core i PostgreSQL.
