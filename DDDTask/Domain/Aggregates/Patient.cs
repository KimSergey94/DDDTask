using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Aggregates;

public class Patient : AggregateRoot<PatientId>
{
    public PersonName Name { get; private set; }
    public ContactInfo ContactInfo { get; private set; }

    private Patient(PatientId id, PersonName name, ContactInfo contactInfo) : base(id)
    {
        Name = name;
        ContactInfo = contactInfo;
    }

    public static Patient Create(PersonName name, ContactInfo contactInfo) =>
        new(PatientId.New(), name, contactInfo);

    public void UpdateContactInfo(ContactInfo contactInfo)
    {
        ContactInfo = contactInfo;
    }
}
