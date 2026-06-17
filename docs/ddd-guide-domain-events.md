# Domain Events — как это работает

## Что такое Domain Event

Domain Event — это факт того, что в домене что-то произошло. Не команда ("сделай это"), а свершившееся событие ("это уже случилось").

Примеры из нашего домена:
- `AppointmentCreatedEvent` — запись была создана
- `SlotBlockedEvent` — специалист заблокировал слот
- `AppointmentCancelledEvent` — запись была отменена

Ключевое слово — **была**. Прошедшее время. Событие нельзя отменить, можно только отреагировать на него.

---

## Что происходит при вызове RaiseDomainEvent

Смотрим реализацию в `AggregateRoot`:

```csharp
// Domain/Shared/AggregateRoot.cs

private readonly List<IDomainEvent> _domainEvents = [];

protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
    _domainEvents.Add(domainEvent);
```

**Ничего особенного.** Событие просто кладётся в список в памяти агрегата.

Когда `Schedule.BlockSlot()` вызывает `RaiseDomainEvent(new SlotBlockedEvent(...))`:

```
Schedule._domainEvents = [ SlotBlockedEvent { SlotId = ..., AffectedAppointmentId = ... } ]
```

Это всё. Письмо написано и лежит в ящике стола. Адресат пока не знает.

---

## Кто читает события и когда

События читает **Application Service** (слой команд/обработчиков) — после того как агрегат сохранён в базу данных.

```
Агрегат.RaiseDomainEvent()  →  событие в _domainEvents (память)
        ↓
Application Service сохраняет агрегат в БД
        ↓
Application Service читает aggregate.DomainEvents
        ↓
Dispatcher публикует события (например через MediatR)
        ↓
EventHandler реагирует: загружает другой агрегат, меняет его, сохраняет
```

---

## Почему диспатч ПОСЛЕ сохранения в БД

Интуитивно кажется: "опубликую событие, обработчик сделает своё дело, потом сохраню". Но это опасно.

**Сценарий с ошибкой если диспатчить ДО сохранения:**

```
1. appointment.CancelByPatient()      ← статус изменён в памяти
2. publisher.Publish(AppointmentCancelledEvent)
3. Обработчик отправляет SMS пациенту ← SMS ушла
4. appointmentRepository.Save()       ← БД упала!
5. Транзакция откатилась              ← статус в БД остался "Confirmed"
6. SMS уже ушла, но запись не отменена ← несогласованность
```

**Правильный порядок — ПОСЛЕ сохранения:**

```
1. appointment.CancelByPatient()      ← статус изменён в памяти
2. appointmentRepository.Save()       ← сохранено в БД ✓
3. publisher.Publish(AppointmentCancelledEvent)
4. Обработчик отправляет SMS          ← SMS ушла, данные в БД согласованы ✓
```

Если после шага 2 упадёт публикация — данные в БД корректны, SMS не дошла. Это лучше, чем наоборот. SMS можно отправить повторно (идемпотентность), а несогласованные данные — нет.

---

## Полная реализация с MediatR (следующий этап)

### Шаг 1: События становятся INotification

MediatR требует чтобы событие реализовывало `INotification`. Добавляем второй интерфейс:

```csharp
// Domain/Events/SlotBlockedEvent.cs

using MediatR;

public record SlotBlockedEvent(
    ScheduleId ScheduleId,
    SpecialistId SpecialistId,
    SlotId SlotId,
    TimeSlot BlockedTime,
    AppointmentId? AffectedAppointmentId) : IDomainEvent, INotification;
                                                          // ↑ добавили
```

### Шаг 2: Application Service диспатчит события после сохранения

```csharp
// Business/Schedules/Commands/BlockSlot/BlockSlotCommandHandler.cs

public class BlockSlotCommandHandler(
    IScheduleRepository scheduleRepository,
    IPublisher publisher) : IRequestHandler<BlockSlotCommand>
{
    public async Task Handle(BlockSlotCommand command, CancellationToken ct)
    {
        var schedule = await scheduleRepository.GetByIdAsync(
            new ScheduleId(command.ScheduleId), ct);

        Guard.Against(schedule is null, "Schedule not found");

        // 1. доменная логика — событие кладётся в _domainEvents
        schedule!.BlockSlot(new SlotId(command.SlotId));

        // 2. сохраняем агрегат
        await scheduleRepository.UpdateAsync(schedule, ct);

        // 3. только после сохранения — диспатчим события
        foreach (var domainEvent in schedule.DomainEvents)
            await publisher.Publish(domainEvent, ct);

        schedule.ClearDomainEvents();
    }
}
```

### Шаг 3: Обработчик события

```csharp
// Business/Schedules/EventHandlers/SlotBlockedEventHandler.cs

public class SlotBlockedEventHandler(
    IAppointmentRepository appointmentRepository) : INotificationHandler<SlotBlockedEvent>
{
    public async Task Handle(SlotBlockedEvent e, CancellationToken ct)
    {
        // Нет затронутой записи — ничего делать не нужно
        if (e.AffectedAppointmentId is null) return;

        var appointment = await appointmentRepository.GetByIdAsync(
            e.AffectedAppointmentId, ct);

        if (appointment is null) return;

        // доменная логика отмены из-за блокировки
        appointment.CancelDueToSlotBlock();

        await appointmentRepository.UpdateAsync(appointment, ct);

        // appointment теперь тоже содержит AppointmentCancelledEvent в DomainEvents
        // его тоже надо диспатчить (например для отправки уведомления пациенту)
        foreach (var domainEvent in appointment.DomainEvents)
            await publisher.Publish(domainEvent, ct);

        appointment.ClearDomainEvents();
    }
}
```

---

## Вспомогательный метод DispatchDomainEvents

Чтобы не дублировать `foreach + ClearDomainEvents` в каждом хэндлере, обычно выносят в extension method:

```csharp
// Shared/Extensions/AggregateRootExtensions.cs

public static class AggregateRootExtensions
{
    public static async Task DispatchDomainEventsAsync<TId>(
        this AggregateRoot<TId> aggregate,
        IPublisher publisher,
        CancellationToken ct = default) where TId : notnull
    {
        foreach (var domainEvent in aggregate.DomainEvents)
            await publisher.Publish(domainEvent, ct);

        aggregate.ClearDomainEvents();
    }
}
```

Тогда в хэндлере просто:

```csharp
await scheduleRepository.UpdateAsync(schedule, ct);
await schedule.DispatchDomainEventsAsync(publisher, ct);
```

---

## Цепочка событий в нашем домене

```
Команда: BlockSlot
    ↓
Schedule.BlockSlot()
    └── RaiseDomainEvent(SlotBlockedEvent)
    ↓
Сохранить Schedule в БД
    ↓
Диспатчить SlotBlockedEvent
    ↓
SlotBlockedEventHandler
    └── Appointment.CancelDueToSlotBlock()
    └── RaiseDomainEvent(AppointmentCancelledEvent)
    └── Сохранить Appointment в БД
    └── Диспатчить AppointmentCancelledEvent
        ↓
        AppointmentCancelledEventHandler
            └── Отправить уведомление пациенту (SMS / email)
```

Каждый обработчик делает одно дело и порождает следующее событие, если нужно. Это и есть **event-driven** подход внутри одного приложения.

---

## Альтернатива: диспатч через Unit of Work

Вместо ручного диспатча в каждом хэндлере — можно сделать так, чтобы `SaveChanges` автоматически публиковал события всех затронутых агрегатов:

```csharp
// Infrastructure/Persistence/AppDbContext.cs

public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var result = await base.SaveChangesAsync(ct);

    // После сохранения — собираем события всех агрегатов
    var aggregates = ChangeTracker.Entries<IAggregateRoot>()
        .Select(e => e.Entity)
        .Where(a => a.DomainEvents.Any())
        .ToList();

    foreach (var aggregate in aggregates)
    {
        foreach (var domainEvent in aggregate.DomainEvents)
            await publisher.Publish(domainEvent, ct);

        aggregate.ClearDomainEvents();
    }

    return result;
}
```

Плюс: не нужно помнить про диспатч в каждом хэндлере.
Минус: диспатч внутри инфраструктурного слоя — смешение ответственностей.

---

## Текущий статус в проекте

Сейчас у нас **только доменный слой**:

| Что есть | Что будет на следующем этапе |
|----------|------------------------------|
| `IDomainEvent` интерфейс | `IDomainEvent : INotification` |
| `RaiseDomainEvent()` в агрегатах | Application Service с `IPublisher` |
| События хранятся в `_domainEvents` | `EventHandler` классы |
| `ClearDomainEvents()` метод | Вызов `DispatchDomainEventsAsync` после Save |

Для задания "чистый доменный слой" — этого достаточно. Инфраструктура и диспатч — следующий слой.
