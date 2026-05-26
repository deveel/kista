using Bogus;

namespace Kista.Entities {
	public class DbTenantPersonFaker : Faker<DbTenantPerson> {
		public DbTenantPersonFaker(string tenantId) {
			RuleFor(x => x.Id, f => Guid.NewGuid());
			RuleFor(x => x.FirstName, f => f.Person.FirstName);
			RuleFor(x => x.LastName, f => f.Person.LastName);
			RuleFor(x => x.Email, f => f.Person.Email.OrNull(f));
			RuleFor(x => x.DateOfBirth, f => f.Person.DateOfBirth);
			RuleFor(x => x.PhoneNumber, f => f.Person.Phone.OrNull(f));
			RuleFor(x => x.TenantId, tenantId);
			RuleFor(x => x.Relationships, f => {
				var faker = new DbTenantPersonRelationshipFaker();
				return f.Random.Bool() ? faker.Generate(f.Random.Number(1, 5)) : null;
			});
		}
	}
}
