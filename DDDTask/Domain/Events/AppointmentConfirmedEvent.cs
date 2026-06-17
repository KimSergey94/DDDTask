using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Events;

public record AppointmentConfirmedEvent(
    AppointmentId AppointmentId,
    PatientId PatientId,
    SpecialistId SpecialistId) : IDomainEvent;
