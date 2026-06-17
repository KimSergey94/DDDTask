using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Events;

public record AppointmentCreatedEvent(
    AppointmentId AppointmentId,
    PatientId PatientId,
    SpecialistId SpecialistId,
    TimeSlot TimeSlot) : IDomainEvent;
