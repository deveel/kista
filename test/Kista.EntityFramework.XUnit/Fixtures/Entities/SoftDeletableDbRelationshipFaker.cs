using Bogus;

namespace Kista.Entities {
	public class SoftDeletableDbRelationshipFaker : Faker<SoftDeletableDbRelationship> {
		public SoftDeletableDbRelationshipFaker() {
			RuleFor(x => x.Type, f => f.PickRandom("friend", "colleague", "family", "spouse"));
			RuleFor(x => x.FullName, f => f.Name.FullName());
		}
	}
}