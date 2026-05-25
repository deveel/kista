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

namespace Kista
{
	/// <summary>
	/// Defines a repository that is bound to a user context,
	/// where the entities are owned by a specific user.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity handled by the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the unique identifier of the entity.
	/// </typeparam>
	/// <typeparam name="TOwnerKey">
	/// The type of the key that identifies the owner of the entity.
	/// </typeparam>
	public interface IUserRepository<TEntity, TKey, TOwnerKey> : IRepository<TEntity, TKey>
		where TEntity : class, IHaveOwner<TOwnerKey>
	{
		/// <summary>
		/// Gets the accessor to the user context of the repository.
		/// </summary>
		IUserAccessor<TOwnerKey> UserAccessor { get; }
	}
}
