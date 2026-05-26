using Bogus;

using NetTopologySuite.Geometries;

namespace Kista.Entities {
	public class DbPersonFaker : Faker<DbPerson> {
		public DbPersonFaker() {
			var relationshipFaker = new DbPersonRelationshipFaker();

			RuleFor(x => x.FirstName, f => f.Name.FirstName());
			RuleFor(x => x.LastName, f => f.Name.LastName());
			RuleFor(x => x.DateOfBirth, f => f.Date.PastOffset(20).UtcDateTime);
			RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f));
			RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber().OrNull(f));
			RuleFor(x => x.Location, f => new Point(f.Random.Double(), f.Random.Double()).OrNull(f));

			RuleFor(x => x.Relationships, (f, p) => {
				return f.Random.Bool() ? null : (IList<DbRelationship>)relationshipFaker.Generate(3);
			});
		}
	}
}
