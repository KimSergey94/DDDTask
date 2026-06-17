using DDDTask.Domain.Enums;
using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Events;

public record AppointmentCancelledEvent(
    AppointmentId AppointmentId,
    PatientId PatientId,
    CancellationReason Reason) : IDomainEvent;
