using Bogus;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace Kista.Owners.XUnit.Unit;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
public class InMemoryUserRepositoryTestSuite
    : UserRepositoryTestSuite<InMemoryUserRepositoryTestSuite.TestBook, Guid, string>
{
    public InMemoryUserRepositoryTestSuite(ITestOutputHelper? outputHelper) : base(outputHelper)
    {
    }

    protected override string GenerateUserId() => Guid.NewGuid().ToString("N");

    protected override Guid GenerateBookId() => Guid.NewGuid();

    protected override Faker<TestBook> BookFaker { get; } = new Faker<TestBook>()
        .RuleFor(b => b.Id, f => f.Random.Guid())
        .RuleFor(b => b.Title, f => f.Lorem.Sentence(3))
        .RuleFor(b => b.Author, f => f.Person.FullName)
        .RuleFor(b => b.Synopsis, f => f.Lorem.Paragraph())
        .RuleFor(b => b.OwnerId, f => f.Random.String2(10));

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddRepositoryContext()
            .AddRepository<TestBookRepo>(repo => repo
                .WithOwnerScoping(),
                ServiceLifetime.Singleton);
    }

    public class TestBook : IBook<Guid>, IHaveOwner<string>
    {
        [Key]
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Synopsis { get; set; }
        public string Author { get; set; } = string.Empty;

        [DataOwner]
        public string? OwnerId { get; set; }

        string IHaveOwner<string>.Owner => OwnerId!;
        void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
    }

    public class TestBookRepo : InMemoryRepository<TestBook, Guid>
    {
        public TestBookRepo(IServiceProvider sp) : base(null, null, sp) { }
    }
}
