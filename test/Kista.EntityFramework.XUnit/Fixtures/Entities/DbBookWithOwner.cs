#pragma warning disable CS8618

namespace Kista.Entities
{
	public class DbBookWithOwner : IBook<Guid>, IHaveOwner<string>
	{
		public Guid Id { get; set; }

		public string Title { get; set; }

		public string? Synopsis { get; set; }

		public string Author { get; set; }

		public string UserId { get; set; }

		string IHaveOwner<string>.Owner => UserId;

		void IHaveOwner<string>.SetOwner(string owner)
		{
			UserId = owner;
		}
	}
}
