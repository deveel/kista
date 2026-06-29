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
	/// Provides configuration options for the repository lifecycle orchestration,
	/// controlling create, drop, seed, and fail-fast behavior.
	/// </summary>
	public class RepositoryLifecycleOptions {
		/// <summary>
		/// Gets or sets whether an existing repository should be dropped and re-created
		/// during initialization. Defaults to <c>true</c>.
		/// </summary>
		public bool DeleteIfExists { get; set; } = true;

		/// <summary>
		/// Gets or sets whether creation should be skipped when the repository already exists.
		/// Defaults to <c>true</c>.
		/// </summary>
		public bool DontCreateExisting { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the orchestrator should throw an exception when no
		/// lifecycle handler is available. Defaults to <c>false</c>.
		/// </summary>
		public bool FailFast { get; set; } = false;

		/// <summary>
		/// Gets or sets the <see cref="SeedStrategy"/> used to determine
		/// if and when seed data is applied. Defaults to <see cref="SeedStrategy.Never"/>.
		/// </summary>
		public SeedStrategy SeedStrategy { get; set; } = SeedStrategy.Never;

		/// <summary>
		/// Gets or sets the name of the hosting environment used to resolve
		/// environment-specific seed strategies. When <c>null</c>, the orchestrator
		/// attempts to resolve the environment name from <c>IHostEnvironment</c>.
		/// </summary>
		public string? EnvironmentName { get; set; }

		/// <summary>
		/// Gets or sets a custom action invoked during seeding instead of the
		/// default handler-based seeding flow. The action receives the service provider,
		/// the entity type, and the seed data.
		/// </summary>
		public Action<IServiceProvider, Type, object?>? SeedAction { get; set; }
	}
}
