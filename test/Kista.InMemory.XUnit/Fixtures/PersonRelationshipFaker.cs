using Bogus;

namespace Kista;

public class PersonRelationshipFaker : Faker<PersonRelationship> {
    public PersonRelationshipFaker() {
        RuleFor(x => x.FullName, f => f.Name.FullName());
        RuleFor(x => x.Type, f => f.PickRandom(new[] { "father", "mother", "brother", "sister" }));
    }
}
