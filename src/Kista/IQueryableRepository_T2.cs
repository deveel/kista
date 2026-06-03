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

using System;

namespace Kista {
	/// <summary>
	/// Represents a repository that is capable of being queried
	/// </summary>
	/// <typeparam name="TEntity">
	/// The strongly typed entity that is stored in the repository
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key used to uniquely identify the entity.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// This contract is obsolete. The <c>AsQueryable()</c> hatch it exposes
	/// leaks the underlying LINQ provider into consumer code and lets
	/// expressions be evaluated outside the data layer, where they can
	/// throw <see cref="NotSupportedException"/> at runtime far from the
	/// repository.
	/// </para>
	/// <para>
	/// Inherit from the abstract <see cref="RepositoryBase{TEntity, TKey}"/>
	/// base class instead: it hides the <see cref="IQueryable{T}"/> hatch
	/// behind a <c>protected abstract</c> member and provides ready-made
	/// <c>FindAsync(IQuery, CancellationToken)</c> and
	/// <c>QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c>
	/// implementations that unpack <see cref="Query"/> and
	/// <see cref="PageQuery{TEntity}"/> inside the data layer.
	/// </para>
	/// </remarks>
	[Obsolete("Use the abstract Kista.RepositoryBase<TEntity, TKey> base class instead. The IQueryable hatch is no longer exposed to consumers.", false)]
	public interface IQueryableRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class {
		/// <summary>
		/// Gets a queryable object that can be used to query the repository
		/// </summary>
		/// <returns>
		/// Returns an instance of <see cref="IQueryable{T}"/> that can be used
		/// to query the repository.
		/// </returns>
		[Obsolete("Use the abstract Kista.RepositoryBase<TEntity, TKey> base class instead. The IQueryable hatch is no longer exposed to consumers.", false)]
		IQueryable<TEntity> AsQueryable();
	}
}