using DDDTask.Domain.Shared;

namespace DDDTask.Domain.ValueObjects;

public class Specialty : ValueObject
{
    public string Value { get; }

    private Specialty(string value)
    {
        Value = value;
    }

    public static Specialty Create(string value)
    {
        Guard.Against(string.IsNullOrWhiteSpace(value), "Specialty cannot be empty");
        return new Specialty(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
