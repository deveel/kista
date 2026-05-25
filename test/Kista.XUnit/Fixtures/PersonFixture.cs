using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Provides shared test infrastructure for <see cref="Person"/> entities,
/// including a centralized <see cref="Faker{T}"/> and an in-memory repository.
/// </summary>
public class PersonFixture
{
    /// <summary>
    /// Per-instance <see cref="Faker{T}"/> for <see cref="Person"/> entities.
    /// Intentionally <em>not</em> <c>static</c>: xUnit parallelises test collections, so a
    /// shared static Faker would be accessed from multiple threads simultaneously, violating
    /// Bogus's single-threaded contract and producing non-deterministic data.
    /// Each <see cref="PersonFixture"/> instance (one per test class) therefore owns its own
    /// Faker with independent random state.
    /// </summary>
    public readonly Faker<Person> PersonFaker = new Faker<Person>("en")
        .RuleFor(p => p.Id, f => f.Random.Guid().ToString())
        .RuleFor(p => p.FirstName, f => f.Name.FirstName())
        .RuleFor(p => p.LastName, f => f.Name.LastName())
        .RuleFor(p => p.Email, f => f.Internet.Email())
        .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber())
        .RuleFor(p => p.DateOfBirth, f => f.Date.Past(30).OrNull(f));

    /// <summary>Generates a single valid <see cref="Person"/>.</summary>
    public Person BuildPerson() => PersonFaker.Generate();

    /// <summary>Generates <paramref name="count"/> valid <see cref="Person"/> instances.</summary>
    public IList<Person> BuildPeople(int count) => PersonFaker.Generate(count);

    /// <summary>
    /// Creates an in-memory repository pre-seeded with <paramref name="count"/> people
    /// and returns both the seeded list and the repository.
    /// </summary>
    public (List<Person> People, IRepository<Person> Repository) BuildSeededRepository(int count = 100)
    {
        var people = PersonFaker.Generate(count);
        var repository = people.AsRepository();
        return (people, repository);
    }
}

