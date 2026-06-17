using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Events;

public record SlotBlockedEvent(
    ScheduleId ScheduleId,
    SpecialistId SpecialistId,
    SlotId SlotId,
    TimeSlot BlockedTime,
    AppointmentId? AffectedAppointmentId) : IDomainEvent;
