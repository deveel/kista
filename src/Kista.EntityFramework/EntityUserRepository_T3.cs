// Copyright 2023-2025 Antonello Provenzano
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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista
{
	/// <summary>
	/// An implementation of a repository that is bound to a specific user
	/// context, where the entities are owned by a specific user.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the unique identifier of the entity.
	/// </typeparam>
	/// <typeparam name="TUserKey">
	/// The type of the key that identifies the owner of the entity.
	/// </typeparam>
	public class EntityUserRepository<TEntity, TKey, TUserKey> : EntityRepository<TEntity, TKey>, IUserRepository<TEntity, TKey, TUserKey>
		where TEntity : class, IHaveOwner<TUserKey>
	{
		/// <summary>
		/// Constructs the repository using the given <see cref="DbContext"/>.
		/// </summary>
		/// <param name="context">
		/// The <see cref="DbContext"/> used to access the data of the entities.
		/// </param>
		/// <param name="userAccessor">
		/// A service used to get the current user context.
		/// </param>
		/// <param name="services">
		/// An optional service provider used to resolve infrastructure services
		/// such as expression caches for filter initialization.
		/// </param>
		/// <param name="logger">
		/// A logger used to log the operations of the repository.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the given <paramref name="userAccessor"/> is <c>null</c>.
		/// </exception>
		public EntityUserRepository(DbContext context, IUserAccessor<TUserKey> userAccessor, IServiceProvider? services = null, ILogger<EntityUserRepository<TEntity, TKey, TUserKey>>? logger = null) : base(context, services, logger)
		{
			ArgumentNullException.ThrowIfNull(userAccessor);
			UserAccessor = userAccessor;
		}

		IUserAccessor<TUserKey> IUserRepository<TEntity, TKey, TUserKey>.UserAccessor => UserAccessor;

		/// <summary>
		/// Gets the accessor to the user context of the repository.
		/// </summary>
		protected IUserAccessor<TUserKey> UserAccessor { get; }

		/// <summary>
		/// Gets the identifier of the current user.
		/// </summary>
		protected TUserKey? UserId => UserAccessor.GetUserId();

		/// <inheritdoc/>
		public override async ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default)
		{
            var userId = UserAccessor.GetUserId();

            if (Equals(default(TUserKey), userId))
            {
                Logger.WarnUserNotSet();
                return null;
            }

			var result = await base.FindAsync(key, cancellationToken);
            
            if (result == null)
            {
                Logger.WarnEntityNotFound(typeof(TEntity), key);
                return null;
            }


            if (!Equals(userId, result.Owner))
            {
                Logger.WarnOwnerNotMatchingUser(typeof(TEntity), key, userId, result.Owner);
                return null;
            }

            return result;
		}

		/// <inheritdoc/>
		protected override TEntity OnAddingEntity(TEntity entity)
		{
			var userId = UserAccessor.GetUserId();
            if (Equals(default(TUserKey), userId))
            {
                Logger.WarnUserNotSet();
            }
            else
            {
                entity.SetOwner(userId!);
            }

			return entity;
		}
	}
}
