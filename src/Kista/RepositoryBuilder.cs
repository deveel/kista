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
