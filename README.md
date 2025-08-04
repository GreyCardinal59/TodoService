# Тестовое задание в компанию Интас

## Запуск
1. Клонировать репозиторий:
```
git clone https://github.com/GreyCardinal59/TodoService.git
cd TodoService\src\TodoApi
```

2. Запустить с помощью Docker Compose:
```
docker-compose up -d
```

3. API доступно по http://localhost:8080

Swagger: http://localhost:8080/swagger/index.html

При запуске
- Создается база данных PostgreSQL
- Применяются миграции и сидирование
- Запуск бэка

## Выполненные задания

### Вариант А: Redis

- закешировал ответы GET
![img](md-sources/Redis1.png)
![img](md-sources/Redis2.png)
- инвалидация кеша при изменении данных
![img](md-sources/invalidation1.png)
![img](md-sources/invalidation2.png)

### Вариант В: RabbitMQ
- Отправка события в RabbitMQ при изменении статуса задачи
![img](md-sources/rabbit1.png)
![img](md-sources/rabbit2.png)

### Вариант С: gRPC-сервис
- gRPC-сервис для статистики:
Клиент создан внутри приложения, в качестве примера создал метод контроллера, чтобы получить ответ клиента
![img](md-sources/grpc.png)
