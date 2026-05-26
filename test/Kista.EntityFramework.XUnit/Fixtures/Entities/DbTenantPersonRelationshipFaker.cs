using Bogus;

namespace Kista.Entities {
	public class DbTenantPersonRelationshipFaker : Faker<DbTenantPersonRelationship> {
		public DbTenantPersonRelationshipFaker() {
			var relTypes = new string[] { "father", "mother", "brother", "sister", "partner" };

			RuleFor(x => x.FullName, f => f.Name.FullName());
			RuleFor(x => x.Type, f => f.PickRandom(relTypes));
		}
	}
}
