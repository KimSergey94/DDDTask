using DDDTask.Domain.Shared;

namespace DDDTask.Domain.ValueObjects;

public class SlotDuration : ValueObject
{
    public TimeSpan Value { get; }

    private SlotDuration(TimeSpan value)
    {
        Value = value;
    }

    public static SlotDuration Create(TimeSpan value)
    {
        Guard.Against(value <= TimeSpan.Zero, "Slot duration must be positive");
        Guard.Against(value > TimeSpan.FromHours(8), "Slot duration cannot exceed 8 hours");
        return new SlotDuration(value);
    }

    public static SlotDuration ThirtyMinutes => Create(TimeSpan.FromMinutes(30));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
