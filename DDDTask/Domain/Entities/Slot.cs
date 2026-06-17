using DDDTask.Domain.Enums;
using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Entities;

// Внутренняя сущность агрегата Schedule — не имеет смысла вне расписания
public class Slot : Entity<SlotId>
{
    public TimeSlot Time { get; private set; }
    public SlotStatus Status { get; private set; }
    public AppointmentId? AppointmentId { get; private set; }

    public bool IsAvailable => Status == SlotStatus.Free;

    internal Slot(SlotId id, TimeSlot time) : base(id)
    {
        Time = time;
        Status = SlotStatus.Free;
    }

    internal void Book(AppointmentId appointmentId)
    {
        Guard.Against(Status != SlotStatus.Free,
            $"Slot {Id.Value} cannot be booked: current status is '{Status}'");

        Status = SlotStatus.Booked;
        AppointmentId = appointmentId;
    }

    internal void Block()
    {
        if (Status == SlotStatus.Blocked) return;
        Status = SlotStatus.Blocked;
    }

    internal void Release()
    {
        Status = SlotStatus.Free;
        AppointmentId = null;
    }
}
