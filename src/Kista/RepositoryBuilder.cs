using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	public class RepositoryBuilder
	{
		public IServiceCollection Services { get; }
		public Type EntityType { get; }
		public Type EntityKeyType { get; }
		public Type RepositoryType { get; }
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
