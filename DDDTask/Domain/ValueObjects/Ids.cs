namespace DDDTask.Domain.ValueObjects;

public sealed record PatientId(Guid Value)
{
    public static PatientId New() => new(Guid.NewGuid());
}

public sealed record SpecialistId(Guid Value)
{
    public static SpecialistId New() => new(Guid.NewGuid());
}

public sealed record AppointmentId(Guid Value)
{
    public static AppointmentId New() => new(Guid.NewGuid());
}

public sealed record ScheduleId(Guid Value)
{
    public static ScheduleId New() => new(Guid.NewGuid());
}

public sealed record SlotId(Guid Value)
{
    public static SlotId New() => new(Guid.NewGuid());
}
