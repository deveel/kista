using Bogus;

namespace Kista.Entities {
	public class SoftDeletableDbPersonFaker : Faker<SoftDeletableDbPerson> {
		public SoftDeletableDbPersonFaker() {
			var relationshipFaker = new SoftDeletableDbRelationshipFaker();

			RuleFor(x => x.FirstName, f => f.Name.FirstName());
			RuleFor(x => x.LastName, f => f.Name.LastName());
			RuleFor(x => x.DateOfBirth, f => f.Date.PastOffset(20).UtcDateTime);
			RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f));
			RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber().OrNull(f));
			RuleFor(x => x.IsDeleted, f => false);
			RuleFor(x => x.DeletedAtUtc, f => (DateTimeOffset?)null);
			RuleFor(x => x.DeletedBy, f => (string?)null);
			RuleFor(x => x.Relationships, (f, p) => {
				return f.Random.Bool() ? null : (IList<SoftDeletableDbRelationship>)relationshipFaker.Generate(3);
			});
		}
	}
}