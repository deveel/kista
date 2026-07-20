using Kista.Caching;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class EntityManagerCachingTests : EntityManagerTests {
    public EntityManagerCachingTests(ITestOutputHelper testOutput) : base(testOutput) {
    }

    protected override void ConfigureServices(IServiceCollection services) {
        services.AddEasyCaching(options => options.UseInMemory("default"));
        services.AddRepositoryContext()
            .AddRepository<InMemoryRepository<Person, string>>(repo => repo
                .WithManagement(mgmt => {
                    mgmt.WithEasyCaching(options => {
                        options.DefaultExpiration = TimeSpan.FromMinutes(15);
                    });
                    mgmt.WithCacheKeyGenerator<PersonCacheKeyGenerator>();
                }));
        base.ConfigureServices(services);
    }
}
