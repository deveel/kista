using Bogus;

namespace Kista {
	public class MongoPersonRelationshipFaker : Faker<MongoPersonRelationship> {
		public MongoPersonRelationshipFaker() {
			RuleFor(x => x.FullName, f => f.Name.FullName());
			RuleFor(x => x.Type, f => f.PickRandom("friend", "family", "colleague"));
		}
	}
}
