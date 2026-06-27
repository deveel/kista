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
	/// Defines the strategy used to seed data into a repository during
	/// the lifecycle initialization phase.
	/// </summary>
	public enum SeedStrategy {
		/// <summary>
		/// No seeding is performed.
		/// </summary>
		Never,

		/// <summary>
		/// Seeding is always performed, regardless of whether the repository exists.
		/// </summary>
		Always,

		/// <summary>
		/// Seeding is performed only if the repository does not already exist.
		/// </summary>
		IfMissing,

		/// <summary>
		/// The seeding strategy is determined by the active environment profile
		/// (see <see cref="IRepositoryLifecycleProfile"/>).
		/// </summary>
		ByEnvironment
	}
}
