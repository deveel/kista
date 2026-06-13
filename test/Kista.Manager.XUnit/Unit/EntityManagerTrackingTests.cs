using System.ComponentModel.DataAnnotations;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "EntityManager")]
public class EntityManagerTrackingTests
{
    [Fact]
    public void Should_ThrowInvalidOperation_When_TrackingRepositoryAccessedForNonTrackingRepository()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new ExposedEntityManager(repository);

        var ex = Assert.Throws<InvalidOperationException>(() => manager.ExposedTrackingRepository);
        Assert.Contains("not tracking", ex.Message);
    }

    [Fact]
    public void Should_ThrowObjectDisposed_When_TrackingRepositoryAccessedAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new ExposedEntityManager(repository);
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.ExposedTrackingRepository);
    }

    private sealed class ExposedEntityManager : EntityManager<Person>
    {
        public ExposedEntityManager(IRepository<Person> repository)
            : base(repository) { }

        public ITrackingRepository<Person, object> ExposedTrackingRepository => TrackingRepository;
    }
}
