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
        services.AddEntityEasyCacheFor<Person>(options => {
            options.Expiration = TimeSpan.FromMinutes(15);
        });
        services.AddEntityCacheKeyGenerator<PersonCacheKeyGenerator>();
        base.ConfigureServices(services);
    }
}
