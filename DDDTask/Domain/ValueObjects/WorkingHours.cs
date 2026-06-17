using DDDTask.Domain.Shared;

namespace DDDTask.Domain.ValueObjects;

public class WorkingHours : ValueObject
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    private WorkingHours(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public static WorkingHours Create(TimeOnly start, TimeOnly end)
    {
        Guard.Against(end <= start, "Working hours end must be after start");
        return new WorkingHours(start, end);
    }

    public bool Contains(TimeSlot slot) =>
        slot.StartTime >= Start && slot.EndTime <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
