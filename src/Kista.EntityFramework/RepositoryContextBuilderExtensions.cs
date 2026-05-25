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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista {
    /// <summary>
	/// Extension methods for configuring the Entity Framework Core driver on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Configures the Entity Framework Core repository driver.
		/// </summary>
		public static EntityFrameworkRepositoryBuilder UseEntityFramework<TDbContext>(this RepositoryContextBuilder builder)
			where TDbContext : DbContext {
			return new EntityFrameworkRepositoryBuilder(builder, typeof(TDbContext));
		}

		/// <summary>
		/// Configures the Entity Framework Core repository driver with a configuration action.
		/// </summary>
		public static RepositoryContextBuilder UseEntityFramework<TDbContext>(this RepositoryContextBuilder builder, Action<EntityFrameworkRepositoryBuilder> configure)
			where TDbContext : DbContext {
			var driverBuilder = new EntityFrameworkRepositoryBuilder(builder, typeof(TDbContext));
			configure(driverBuilder);
			driverBuilder.FinalizeRegistration();
			return builder;
		}
	}
}
