using Bogus;

namespace Kista {
	public class PersonFaker : Faker<Person> {
		public PersonFaker() {
			RuleFor(x => x.FirstName, f => f.Name.FirstName());
			RuleFor(x => x.LastName, f => f.Name.LastName());
			RuleFor(x => x.DateOfBirth, f => f.Date.Past(20).OrNull(f));
			RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f));
			RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber().OrNull(f));
		}
	}
}
