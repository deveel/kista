using Bogus;

namespace Kista;

/// <summary>
/// A <see cref="Faker{T}"/> for <see cref="Person"/> entities.
/// Prefer using <see cref="PersonFixture.PersonFaker"/> directly in fixtures;
/// this class exists for convenience and backward compatibility.
/// </summary>
public class PersonFaker : Faker<Person>
{
    public PersonFaker()
    {
        RuleFor(p => p.Id, f => f.Random.Guid().ToString());
        RuleFor(p => p.FirstName, f => f.Name.FirstName());
        RuleFor(p => p.LastName, f => f.Name.LastName());
        RuleFor(p => p.Email, f => f.Internet.Email());
        RuleFor(p => p.Phone, f => f.Phone.PhoneNumber());
        RuleFor(p => p.DateOfBirth, f => f.Date.Past(30).OrNull(f));
    }
}
