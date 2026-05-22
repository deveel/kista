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

namespace Deveel.Data {
    /// <summary>
	/// Extension methods for configuring the In-Memory driver on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Configures the In-Memory repository driver.
		/// </summary>
		public static InMemoryRepositoryBuilder UseInMemory(this RepositoryContextBuilder builder) {
			return new InMemoryRepositoryBuilder(builder);
		}

		/// <summary>
		/// Configures the In-Memory repository driver with a configuration action.
		/// </summary>
		public static RepositoryContextBuilder UseInMemory(this RepositoryContextBuilder builder, Action<InMemoryRepositoryBuilder> configure) {
			var driverBuilder = new InMemoryRepositoryBuilder(builder);
			configure(driverBuilder);
			return builder;
		}
	}
}
