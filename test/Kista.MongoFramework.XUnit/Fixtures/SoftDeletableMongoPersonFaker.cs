using Bogus;

namespace Kista {
	public class SoftDeletableMongoPersonFaker : Faker<SoftDeletableMongoPerson> {
		public SoftDeletableMongoPersonFaker() {
			RuleFor(x => x.FirstName, f => f.Name.FirstName());
			RuleFor(x => x.LastName, f => f.Name.LastName().OrNull(f));
			RuleFor(x => x.DateOfBirth, f => f.Date.Past(20));
			RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f));
			RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber().OrNull(f));
			RuleFor(x => x.IsDeleted, f => false);
			RuleFor(x => x.DeletedAtUtc, f => (DateTimeOffset?)null);
			RuleFor(x => x.DeletedBy, f => (string?)null);
			RuleFor(x => x.Relationships, f => {
				var faker = new MongoPersonRelationshipFaker();
				return f.Random.Bool() ? faker.Generate(f.Random.Number(1, 5)) : null;
			});
		}
	}
}