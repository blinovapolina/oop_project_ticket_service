# Система бронирования билетов

Микросервисная система онлайн-продажи билетов на концерты. Разработана на ASP.NET Core Web API с использованием PostgreSQL, Redis и Docker.

## Архитектура

| Сервис             | Порт | Ответственность                                                           |
| ------------------ | ---- | ------------------------------------------------------------------------- |
| **EventsService**  | 5001 | События, площадки, места, резервирование/подтверждение/освобождение мест  |
| **BookingService** | 5002 | Бронирования, mock-платежи, билеты, фоновая задача истечения бронирований |
| **PostgreSQL**     | 5432 | `events_db` + `bookings_db`                                               |
| **Redis**          | 6379 | Кэш списка событий + индекс истечения бронирований                        |

```
Client → EventsService → PostgreSQL
Client → BookingService → PostgreSQL
BookingService → EventsService (HTTP)
EventsService → Redis (cache published events)
BookingService → Redis (sorted set for booking TTL)
```

## Требования

* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (рекомендуется)
* Или .NET 9 SDK + Docker только для инфраструктуры

## Быстрый запуск через Docker

```bash
# Из корня проекта
docker compose up --build
```

Сервисы будут доступны по адресам:

| Сервис          | URL                                                       |
| --------------- | --------------------------------------------------------- |
| EventsService   | http://localhost:5001                                     |
| BookingService  | http://localhost:5002                                     |
| Events Swagger  | http://localhost:5001/swagger                             |
| Booking Swagger | http://localhost:5002/swagger                             |
| PostgreSQL      | localhost:5433 (на хосте) / postgres:5432 (внутри Docker) |
| Redis           | localhost:6379                                            |

Остановить все сервисы:

```bash
docker compose down
```

Удалить volumes и сбросить базы данных:

```bash
docker compose down -v
```

## Локальная разработка без запуска приложений в Docker

Запустить только инфраструктуру:

```bash
docker compose up postgres redis -d
```

Затем запустить сервисы локально:

```bash
cd src/EventsService && dotnet run
cd src/BookingService && dotnet run
```

Строки подключения в `appsettings.json` указывают на `localhost:5432` и `localhost:6379`.

## Начальные данные

При первом запуске EventsService добавляет начальные данные:

| Сущность              | ID                                     |
| --------------------- | -------------------------------------- |
| Площадка              | `11111111-1111-1111-1111-111111111111` |
| Событие (Published)   | `22222222-2222-2222-2222-222222222222` |
| Пользователь Customer | `33333333-3333-3333-3333-333333333333` |
| Место события 1 (VIP) | `bbbbbbbb-bbbb-bbbb-bbbb-000000000001` |
| Место события 2 (VIP) | `bbbbbbbb-bbbb-bbbb-bbbb-000000000002` |

## Аутентификация (MVP)

| Заголовок                 | Назначение                                     |
| ------------------------- | ---------------------------------------------- |
| `X-User-Id`               | Обязателен для endpoints бронирования          |
| `X-User-Role: Admin`      | Обязателен для `POST /api/events`              |
| `X-Mock-Payment: success` | Mock-платёж завершается успешно (по умолчанию) |
| `X-Mock-Payment: fail`    | Mock-платёж завершается ошибкой → 502          |

## Основной E2E-сценарий

```bash
EVENT_ID="22222222-2222-2222-2222-222222222222"
USER_ID="33333333-3333-3333-3333-333333333333"
SEAT1="bbbbbbbb-bbbb-bbbb-bbbb-000000000001"

curl http://localhost:5001/api/events
curl "http://localhost:5001/api/events/$EVENT_ID/seats"

BOOKING=$(curl -s -X POST http://localhost:5002/api/bookings \
  -H "Content-Type: application/json" \
  -H "X-User-Id: $USER_ID" \
  -d "{\"userId\":\"$USER_ID\",\"eventId\":\"$EVENT_ID\",\"eventSeatIds\":[\"$SEAT1\"]}")

BOOKING_ID=$(echo "$BOOKING" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")

curl -X POST "http://localhost:5002/api/bookings/$BOOKING_ID/pay" \
  -H "X-User-Id: $USER_ID" \
  -H "X-Mock-Payment: success"

curl "http://localhost:5002/api/bookings/$BOOKING_ID" -H "X-User-Id: $USER_ID"
```

## Использование Redis

| Ключ                | Сервис         | Назначение                                  |
| ------------------- | -------------- | ------------------------------------------- |
| `events:published`  | EventsService  | Кэш для GET `/api/events` (TTL 5 минут)     |
| `bookings:expiring` | BookingService | Sorted set: bookingId → timestamp expiresAt |

## Сервисы Docker Compose

| Контейнер                | Image / Build                             |
| ------------------------ | ----------------------------------------- |
| `ticket-postgres`        | postgres:16-alpine                        |
| `ticket-redis`           | redis:7-alpine                            |
| `ticket-events-service`  | Сборка из `src/EventsService/Dockerfile`  |
| `ticket-booking-service` | Сборка из `src/BookingService/Dockerfile` |

## Структура проекта

```
OOP_project/
├── docker-compose.yml
├── docker/postgres/init.sql
├── src/
│   ├── EventsService/
│   └── BookingService/
└── README.md
```

## Лицензия

Учебный проект по курсу ООП (Backboost 2026).
