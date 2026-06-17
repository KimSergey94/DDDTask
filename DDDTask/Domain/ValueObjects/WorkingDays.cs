using DDDTask.Domain.Shared;

namespace DDDTask.Domain.ValueObjects;

public class WorkingDays : ValueObject
{
    private readonly IReadOnlySet<DayOfWeek> _days;

    public IEnumerable<DayOfWeek> Days => _days;

    private WorkingDays(IReadOnlySet<DayOfWeek> days)
    {
        _days = days;
    }

    public static WorkingDays Create(IEnumerable<DayOfWeek> days)
    {
        var set = new HashSet<DayOfWeek>(days);
        Guard.Against(set.Count == 0, "Working days must include at least one day");
        return new WorkingDays(set);
    }

    public bool Includes(DayOfWeek day) => _days.Contains(day);

    protected override IEnumerable<object?> GetEqualityComponents() =>
        _days.OrderBy(d => (int)d).Cast<object?>();
}
