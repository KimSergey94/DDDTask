using DDDTask.Domain.Shared;

namespace DDDTask.Domain.ValueObjects;

public class PersonName : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static PersonName Create(string firstName, string lastName)
    {
        Guard.Against(string.IsNullOrWhiteSpace(firstName), "First name cannot be empty");
        Guard.Against(string.IsNullOrWhiteSpace(lastName), "Last name cannot be empty");
        return new PersonName(firstName.Trim(), lastName.Trim());
    }

    public string Full => $"{LastName} {FirstName}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }
}
