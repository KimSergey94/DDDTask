using DDDTask.Domain.Enums;
using DDDTask.Domain.Events;
using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Aggregates;

public class Appointment : AggregateRoot<AppointmentId>
{
    private static readonly TimeSpan PatientCancellationWindow = TimeSpan.FromHours(2);

    public PatientId PatientId { get; private set; }
    public SpecialistId SpecialistId { get; private set; }
    public SlotId SlotId { get; private set; }
    public TimeSlot TimeSlot { get; private set; }
    public AppointmentStatus Status { get; private set; }

    private Appointment(
        AppointmentId id,
        PatientId patientId,
        SpecialistId specialistId,
        SlotId slotId,
        TimeSlot timeSlot) : base(id)
    {
        PatientId = patientId;
        SpecialistId = specialistId;
        SlotId = slotId;
        TimeSlot = timeSlot;
        Status = AppointmentStatus.Created;
    }

    public static Appointment Create(
        PatientId patientId,
        SpecialistId specialistId,
        SlotId slotId,
        TimeSlot timeSlot)
    {
        var appointment = new Appointment(AppointmentId.New(), patientId, specialistId, slotId, timeSlot);
        appointment.RaiseDomainEvent(new AppointmentCreatedEvent(
            appointment.Id, patientId, specialistId, timeSlot));
        return appointment;
    }

    // Инвариант: пациент может отменить не позже чем за 2 часа до приёма
    public void CancelByPatient(DateTime cancelledAt)
    {
        Guard.Against(
            Status is AppointmentStatus.CancelledByPatient or AppointmentStatus.CancelledByStaff or AppointmentStatus.Completed,
            $"Cannot cancel appointment with status '{Status}'");

        Guard.Against(
            TimeSlot.Start - cancelledAt < PatientCancellationWindow,
            $"Patient can only cancel at least {PatientCancellationWindow.TotalHours}h before the appointment. " +
            $"Starts at {TimeSlot.Start:g}, now: {cancelledAt:g}");

        Status = AppointmentStatus.CancelledByPatient;
        RaiseDomainEvent(new AppointmentCancelledEvent(Id, PatientId, CancellationReason.CancelledByPatient));
    }

    // Специалист или администратор — без ограничения по времени
    public void CancelByStaff()
    {
        if (Status is AppointmentStatus.CancelledByPatient or AppointmentStatus.CancelledByStaff)
            return; // idempotent

        Guard.Against(Status == AppointmentStatus.Completed, "Cannot cancel a completed appointment");

        Status = AppointmentStatus.CancelledByStaff;
        RaiseDomainEvent(new AppointmentCancelledEvent(Id, PatientId, CancellationReason.CancelledByStaff));
    }

    // Вызывается обработчиком события SlotBlocked
    public void CancelDueToSlotBlock()
    {
        if (Status is AppointmentStatus.CancelledByPatient or AppointmentStatus.CancelledByStaff or AppointmentStatus.Completed)
            return;

        Status = AppointmentStatus.CancelledByStaff;
        RaiseDomainEvent(new AppointmentCancelledEvent(Id, PatientId, CancellationReason.SlotBlocked));
    }

    // Инвариант: отменённую запись нельзя подтвердить — только создать новую
    public void Confirm()
    {
        Guard.Against(
            Status is AppointmentStatus.CancelledByPatient or AppointmentStatus.CancelledByStaff,
            "Cannot confirm a cancelled appointment. Create a new appointment instead.");

        Guard.Against(
            Status != AppointmentStatus.Created,
            $"Cannot confirm appointment with status '{Status}'");

        Status = AppointmentStatus.Confirmed;
        RaiseDomainEvent(new AppointmentConfirmedEvent(Id, PatientId, SpecialistId));
    }

    public void Complete()
    {
        Guard.Against(
            Status != AppointmentStatus.Confirmed,
            $"Cannot complete appointment with status '{Status}'. Must be Confirmed first.");

        Status = AppointmentStatus.Completed;
    }
}
