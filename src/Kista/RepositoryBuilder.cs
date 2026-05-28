using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	/// <summary>
	/// Carries type metadata for a registered repository, enabling
	/// further configuration via extension methods (e.g. owner scoping, seeding).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Returned by <c>AddRepository&lt;TRepository&gt;()</c> on <see cref="RepositoryContextBuilder"/>.
	/// It exposes the entity type, key type, repository type, and the primary service interface
	/// so that generic extension methods can construct the correct closed types.
	/// </para>
	/// </remarks>
	public class RepositoryBuilder
	{
		/// <summary>
		/// Gets the underlying <see cref="IServiceCollection"/> for direct registration.
		/// </summary>
		public IServiceCollection Services { get; }

		/// <summary>
		/// Gets the entity type managed by the repository.
		/// </summary>
		public Type EntityType { get; }

		/// <summary>
		/// Gets the type of the entity's primary key.
		/// </summary>
		public Type EntityKeyType { get; }

		/// <summary>
		/// Gets the concrete repository type that was registered.
		/// </summary>
		public Type RepositoryType { get; }

		/// <summary>
		/// Gets the primary service interface type (typically <c>IRepository&lt;TEntity, TKey&gt;</c>).
		/// </summary>
		public Type ServiceType { get; }

		internal RepositoryBuilder(
			IServiceCollection services,
			Type entityType,
			Type entityKeyType,
			Type repositoryType,
			Type serviceType)
		{
			Services = services;
			EntityType = entityType;
			EntityKeyType = entityKeyType;
			RepositoryType = repositoryType;
			ServiceType = serviceType;
		}
	}
}
