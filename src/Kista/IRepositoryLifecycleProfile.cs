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


namespace Kista {
	/// <summary>
	/// Defines a lifecycle profile that provides environment-specific
	/// seed strategies and seed data for repository initialization.
	/// </summary>
	public interface IRepositoryLifecycleProfile {
		/// <summary>
		/// Gets the <see cref="SeedStrategy"/> to use for the given environment.
		/// </summary>
		/// <param name="environmentName">
		/// The name of the hosting environment, or <c>null</c> if unknown.
		/// </param>
		/// <returns>
		/// The <see cref="SeedStrategy"/> appropriate for the environment.
		/// </returns>
		SeedStrategy GetSeedStrategy(string? environmentName = null);

		/// <summary>
		/// Gets the seed data for the given entity type by <see cref="Type"/>.
		/// </summary>
		/// <param name="entityType">The type of the entity to seed.</param>
		/// <returns>
		/// Seed data for the entity type, or <c>null</c> if none is available.
		/// </returns>
		object? GetSeedData(Type entityType);
	}
}
