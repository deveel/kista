using Bogus;

namespace Kista;

public class PersonFaker : Faker<Person> {
    public PersonFaker() {
        RuleFor(x => x.FirstName, f => f.Name.FirstName());
        RuleFor(x => x.LastName, f => f.Name.LastName().OrNull(f));
        RuleFor(x => x.DateOfBirth, f => f.Date.Past(20));
        RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f));
        RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber().OrNull(f));
        RuleFor(x => x.Relationships, f => {
            var faker = new PersonRelationshipFaker();
            return f.Random.Bool() ? faker.GenerateBetween(1, 3) : null;
        });
    }
}
