using Kista;
using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	public static class RepositoryBuilderExtensions
	{
		public static RepositoryBuilder WithOwnerScoping(
			this RepositoryBuilder builder,
			Action<UserScopingOptions>? configure = null)
		{
			var userKeyType = FindUserKeyType(builder.EntityType)
				?? throw new InvalidOperationException(
					$"Entity {builder.EntityType.Name} does not implement IHaveOwner<TUserKey>");

			var decoratorType = typeof(UserScopedRepositoryDecorator<,,>)
				.MakeGenericType(builder.EntityType, builder.EntityKeyType, userKeyType);

			var options = new UserScopingOptions();
			configure?.Invoke(options);

			builder.Services.AddSingleton(options);
			builder.Services.Decorate(builder.ServiceType, decoratorType);

			return builder;
		}

		private static Type? FindUserKeyType(Type entityType)
		{
			foreach (var iface in entityType.GetInterfaces())
			{
				if (!iface.IsGenericType) continue;

				var genericDef = iface.GetGenericTypeDefinition();
				if (genericDef == typeof(IHaveOwner<>) ||
					genericDef.FullName == "Kista.IHaveOwner`1")
				{
					return iface.GetGenericArguments()[0];
				}
			}
			return null;
		}
	}
}
