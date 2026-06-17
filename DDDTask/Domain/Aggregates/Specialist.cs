using DDDTask.Domain.Shared;
using DDDTask.Domain.ValueObjects;

namespace DDDTask.Domain.Aggregates;

public class Specialist : AggregateRoot<SpecialistId>
{
    public PersonName Name { get; private set; }
    public Specialty Specialty { get; private set; }

    private Specialist(SpecialistId id, PersonName name, Specialty specialty) : base(id)
    {
        Name = name;
        Specialty = specialty;
    }

    public static Specialist Create(PersonName name, Specialty specialty) =>
        new(SpecialistId.New(), name, specialty);
}
