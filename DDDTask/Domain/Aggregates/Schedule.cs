using DDDTask.Domain.Entities;
using DDDTask.Domain.Events;
using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Aggregates;

public class Schedule : AggregateRoot<ScheduleId>
{
    public SpecialistId SpecialistId { get; private set; }
    public WorkingHours WorkingHours { get; private set; }
    public WorkingDays WorkingDays { get; private set; }
    public SlotDuration SlotDuration { get; private set; }

    private readonly List<Slot> _slots = [];
    public IReadOnlyList<Slot> Slots => _slots.AsReadOnly();

    private Schedule(
        ScheduleId id,
        SpecialistId specialistId,
        WorkingHours workingHours,
        WorkingDays workingDays,
        SlotDuration slotDuration) : base(id)
    {
        SpecialistId = specialistId;
        WorkingHours = workingHours;
        WorkingDays = workingDays;
        SlotDuration = slotDuration;
    }

    public static Schedule Create(
        SpecialistId specialistId,
        WorkingHours workingHours,
        WorkingDays workingDays,
        SlotDuration slotDuration) =>
        new(ScheduleId.New(), specialistId, workingHours, workingDays, slotDuration);

    // Инвариант: слот должен быть в рабочий день, в рамках рабочих часов, без пересечений
    public void AddSlot(TimeSlot time)
    {
        Guard.Against(!WorkingDays.Includes(time.Start.DayOfWeek),
            $"{time.Start.DayOfWeek} is not a working day for this specialist");

        Guard.Against(!WorkingHours.Contains(time),
            $"Slot {time.StartTime}–{time.EndTime} is outside working hours {WorkingHours.Start}–{WorkingHours.End}");

        Guard.Against(time.Duration != SlotDuration.Value,
            $"Slot duration {time.Duration.TotalMinutes} min doesn't match schedule slot duration {SlotDuration.Value.TotalMinutes} min");

        Guard.Against(_slots.Any(s => s.Time.Overlaps(time)),
            $"Slot {time.Start:g}–{time.End:g} overlaps with an existing slot");

        _slots.Add(new Slot(SlotId.New(), time));
    }

    public void BookSlot(SlotId slotId, AppointmentId appointmentId)
    {
        var slot = FindSlotOrThrow(slotId);
        slot.Book(appointmentId);
    }

    // Инвариант: при блокировке порождается событие — все записи на слот отменяются
    public void BlockSlot(SlotId slotId)
    {
        var slot = FindSlotOrThrow(slotId);
        var affectedAppointmentId = slot.AppointmentId;

        slot.Block();

        RaiseDomainEvent(new SlotBlockedEvent(
            Id, SpecialistId, slotId, slot.Time, affectedAppointmentId));
    }

    public void ReleaseSlot(SlotId slotId)
    {
        var slot = FindSlotOrThrow(slotId);
        slot.Release();
    }

    public IEnumerable<Slot> GetFreeSlots() => _slots.Where(s => s.IsAvailable);

    public Slot? FindSlot(SlotId slotId) => _slots.FirstOrDefault(s => s.Id == slotId);

    private Slot FindSlotOrThrow(SlotId slotId) =>
        FindSlot(slotId)
        ?? throw new DomainException($"Slot {slotId.Value} not found in schedule {Id.Value}");
}
