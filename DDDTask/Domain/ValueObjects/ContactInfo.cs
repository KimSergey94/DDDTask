using DDDTask.Domain.Shared;

namespace DDDTask.Domain.ValueObjects;

public class ContactInfo : ValueObject
{
    public string? Phone { get; }
    public string? Email { get; }

    private ContactInfo(string? phone, string? email)
    {
        Phone = phone;
        Email = email;
    }

    public static ContactInfo Create(string? phone, string? email)
    {
        var trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        Guard.Against(trimmedPhone is null && trimmedEmail is null,
            "At least one contact — phone or email — is required");

        return new ContactInfo(trimmedPhone, trimmedEmail);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Phone;
        yield return Email;
    }
}
