
namespace Kista
{
	public interface IBook<TKey>
	{
		TKey? Id { get; set; }

		string Title { get; set; }

		string? Synopsis { get; set; }

		string Author { get; set; }
	}
}
