using Bogus;

namespace Kista.Entities
{
	public class DbBookFaker : Faker<DbBookWithOwner>
	{
		public DbBookFaker(string userId)
		{
			RuleFor(x => x.Title, f => f.Lorem.Sentence());
			RuleFor(x => x.Author, f => f.Name.FullName());
			RuleFor(x => x.Synopsis, f => f.Lorem.Paragraph());
			RuleFor(x => x.UserId, userId);
		}
	}
}
