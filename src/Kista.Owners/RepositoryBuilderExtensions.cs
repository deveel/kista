// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	/// <summary>
	/// Extension methods for <see cref="RepositoryBuilder"/> to enable
	/// user-scoped repository decoration.
	/// </summary>
	public static class RepositoryBuilderExtensions
	{
		/// <summary>
		/// Wraps the registered repository with a <see cref="UserScopedRepositoryDecorator{TEntity, TKey, TUserKey}"/>
		/// that automatically assigns the current user as owner on writes and filters all reads by owner.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The decorator is registered via Scrutor's <c>Decorate</c> method, so the original
		/// repository remains resolvable as-is and the decorator intercepts all operations
		/// transparently.
		/// </para>
		/// <para>
		/// The user key type (<c>TUserKey</c>) is inferred from the <see cref="IHaveOwner{TKey}"/>
		/// interface on the entity type. The entity must implement <c>IHaveOwner&lt;TUserKey&gt;</c>.
		/// </para>
		/// </remarks>
		/// <param name="builder">The repository builder to configure.</param>
		/// <param name="configure">
		/// Optional delegate to configure <see cref="UserScopingOptions"/> (e.g. <see cref="UserScopingOptions.ThrowWhenUserNotSet"/>).
		/// </param>
		/// <returns>The same builder for chaining.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the entity type does not implement <see cref="IHaveOwner{TKey}"/>.
		/// </exception>
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
			foreach (var iface in entityType.GetInterfaces().Where(iface => iface.IsGenericType))
			{
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
