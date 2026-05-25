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
	/// that owns the entities.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the repository.
	/// </typeparam>
	/// <typeparam name="TUserKey">
	/// The type of the key that identifies the owner of the entity.
	/// </typeparam>
	/// <remarks>
	/// This version of the repository is assuming the type of the key of the entity
	/// of the repository is <see cref="object"/>.
	/// </remarks>
	/// <seealso cref="EntityUserRepository{TEntity, TKey, TUserKey}"/>
	public class EntityUserRepository<TEntity, TUserKey> : EntityUserRepository<TEntity, object, TUserKey>
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
		public EntityUserRepository(DbContext context, IUserAccessor<TUserKey> userAccessor, IServiceProvider? services = null, ILogger<EntityUserRepository<TEntity, object, TUserKey>>? logger = null) 
			: base(context, userAccessor, services, logger)
		{
		}
	}
}
