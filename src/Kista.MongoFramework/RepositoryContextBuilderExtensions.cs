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

using MongoFramework;

namespace Kista {
    /// <summary>
	/// Extension methods for configuring the MongoDB driver on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Configures the MongoDB repository driver.
		/// </summary>
		public static MongoRepositoryBuilder UseMongoDB<TContext>(this RepositoryContextBuilder builder)
			where TContext : class, IMongoDbContext {
			return new MongoRepositoryBuilder(builder, typeof(TContext));
		}

		/// <summary>
		/// Configures the MongoDB repository driver with a configuration action.
		/// </summary>
		public static RepositoryContextBuilder UseMongoDB<TContext>(this RepositoryContextBuilder builder, Action<MongoRepositoryBuilder> configure)
			where TContext : class, IMongoDbContext {
			var driverBuilder = new MongoRepositoryBuilder(builder, typeof(TContext));
			configure(driverBuilder);
			driverBuilder.FinalizeRegistration();
			return builder;
		}
	}
}
