using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	/// <summary>
	/// Extension methods for <see cref="RepositoryBuilder"/> to configure
	/// seed data providers scoped to a specific repository registration.
	/// </summary>
	public static class RepositoryBuilderSeedExtensions
	{
		/// <summary>
		/// Registers a seed data provider for the entity type associated with this builder.
		/// </summary>
		/// <typeparam name="TProvider">
		/// The type implementing <see cref="IRepositorySeedDataProvider{TEntity}"/>.
		/// </typeparam>
		/// <param name="builder">The repository builder to configure.</param>
		/// <param name="lifetime">The service lifetime (default: <see cref="ServiceLifetime.Singleton"/>).</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryBuilder WithSeedData<TProvider>(this RepositoryBuilder builder, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TProvider : class
		{
			var providerType = typeof(IRepositorySeedDataProvider<>).MakeGenericType(builder.EntityType);
			builder.Services.Add(ServiceDescriptor.Describe(providerType, typeof(TProvider), lifetime));
			return builder;
		}

		/// <summary>
		/// Registers inline seed data for the entity type associated with this builder.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
		/// <param name="builder">The repository builder to configure.</param>
		/// <param name="data">The seed data to register.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryBuilder WithSeedData<TEntity>(this RepositoryBuilder builder, IEnumerable<TEntity> data)
			where TEntity : class
		{
			builder.Services.AddSingleton<IRepositorySeedDataProvider<TEntity>>(
				new RepositoryContextBuilder.CollectionSeedDataProvider<TEntity>(data));
			return builder;
		}
	}
}
