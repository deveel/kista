using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	public static class RepositoryBuilderSeedExtensions
	{
		public static RepositoryBuilder WithSeedData<TProvider>(this RepositoryBuilder builder, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TProvider : class
		{
			var providerType = typeof(IRepositorySeedDataProvider<>).MakeGenericType(builder.EntityType);
			builder.Services.Add(ServiceDescriptor.Describe(providerType, typeof(TProvider), lifetime));
			return builder;
		}

		public static RepositoryBuilder WithSeedData<TEntity>(this RepositoryBuilder builder, IEnumerable<TEntity> data)
			where TEntity : class
		{
			builder.Services.AddSingleton<IRepositorySeedDataProvider<TEntity>>(
				new RepositoryContextBuilder.CollectionSeedDataProvider<TEntity>(data));
			return builder;
		}
	}
}
