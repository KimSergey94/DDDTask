using DDDTask.Domain.Shared;

namespace DDDTask.Domain.ValueObjects;

public class TimeSlot : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private TimeSlot(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    public static TimeSlot Create(DateTime start, DateTime end)
    {
        Guard.Against(end <= start, "Slot end time must be after start time");
        return new TimeSlot(start, end);
    }

    public TimeSpan Duration => End - Start;

    public bool Overlaps(TimeSlot other) =>
        Start < other.End && End > other.Start;

    public TimeOnly StartTime => TimeOnly.FromDateTime(Start);
    public TimeOnly EndTime => TimeOnly.FromDateTime(End);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
