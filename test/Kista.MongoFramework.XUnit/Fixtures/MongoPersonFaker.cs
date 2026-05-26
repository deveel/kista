using Bogus;

using MongoDB.Driver.GeoJsonObjectModel;

namespace Kista {
	public class MongoPersonFaker : Faker<MongoPerson> {
		public MongoPersonFaker() {
			RuleFor(x => x.FirstName, f => f.Name.FirstName());
			RuleFor(x => x.LastName, f => f.Name.LastName().OrNull(f));
			RuleFor(x => x.DateOfBirth, f => f.Date.Past(20));
			RuleFor(x => x.Description, f => f.Lorem.Sentence().OrNull(f));
			RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f));
			RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber().OrNull(f));
			RuleFor(x => x.Location, f => (new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(f.Random.Double(), f.Random.Double()))).OrNull(f));

			RuleFor(x => x.Relationships, f => {
				var faker = new MongoPersonRelationshipFaker();
				return f.Random.Bool() ? faker.Generate(f.Random.Number(1, 5)) : null;
			});
		}
	}
}
