---
title: Testing And CI
type: testing
tags:
  - distributed-chat
  - testing
  - ci
---

# Testing And CI

[[00-Start|Powrót do indeksu]]

## Testy backendu

Katalogi:

```text
backend/tests/DistributedChat.UnitTests/
backend/tests/DistributedChat.IntegrationTests/
```

Przykładowe obszary testów unit:

- architektura zależności projektów,
- JWT token generator,
- password hasher,
- walidatory requestów,
- result mapper,
- kontrakty zdarzeń.

Przykładowe obszary testów integracyjnych:

- endpointy auth,
- endpointy rooms,
- SignalR hub,
- status API,
- persistence EF Core,
- RabbitMQ publisher/consumer.

## Testy frontendu

Frontend używa Vitest:

```powershell
cd frontend
npm test
```

Lint:

```powershell
npm run lint
```

Build:

```powershell
npm run build
```

Format check:

```powershell
npm run format:check
```

## Komendy walidacyjne backendu

```powershell
dotnet restore
dotnet build DistributedChat.slnx
dotnet test DistributedChat.slnx
dotnet format DistributedChat.slnx --verify-no-changes
```

## GitHub Actions

Workflowy:

- `.github/workflows/pull-request-ci.yml`
- `.github/workflows/main-ci-cd.yml`

### Pull Request CI

Dla PR po przełączeniu z draft na ready for review uruchamiane są:

- backend restore/build/test/format,
- frontend npm ci/lint/test/build,
- Docker Compose config i build obrazów bez publikacji.

Workflow ma concurrency z `cancel-in-progress`.

### Main CI

Po pushu do `main`:

- buduje i testuje backend,
- lintuje/testuje/buduje frontend,
- buduje obrazy Docker,
- publikuje obrazy do GHCR.

Publikowane obrazy:

- `ghcr.io/<owner>/distributed-chat-api`
- `ghcr.io/<owner>/distributed-chat-web`

Tagi:

- `latest`
- pełny SHA commita

## Zasady dodawania testów

- Dla reguł domenowych i walidacji preferuj testy unit.
- Dla EF Core, PostgreSQL, RabbitMQ i pełnego API preferuj testy integracyjne.
- Przy zmianie kontraktu API dodaj test endpointu i sprawdź frontend.
- Przy zmianie zdarzeń RabbitMQ dodaj test kontraktu oraz publisher/consumer.

## Powiązane notatki

- [[02-Architecture]]
- [[03-Backend]]
- [[04-Frontend]]
- [[07-Local-Development]]
