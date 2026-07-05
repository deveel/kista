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
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista {
	/// <inheritdoc cref="ServiceCollectionExtensions"/>
	public static partial class ServiceCollectionExtensions {
		/// <summary>
		/// Registers soft-delete configuration for the repository context,
		/// enabling any future options knobs (for example a <c>DeletedBy</c>
		/// accessor) to be resolved from the service provider.
		/// </summary>
		/// <param name="builder">
		/// The repository context builder to configure.
		/// </param>
		/// <param name="configure">
		/// An optional delegate to configure the <see cref="SoftDeleteOptions"/>.
		/// </param>
		/// <returns>
		/// Returns the same builder for chaining.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Soft-delete filtering activates automatically for any entity
		/// implementing <see cref="ISoftDeletable"/>: this call is not
		/// required to enable filtering. It is reserved for future
		/// configuration knobs and for symmetry with the per-entity
		/// <see cref="WithSoftDelete(RepositoryBuilder, Action{SoftDeleteOptions}?)"/>.
		/// </para>
		/// </remarks>
		public static RepositoryContextBuilder WithSoftDelete(this RepositoryContextBuilder builder, Action<SoftDeleteOptions>? configure = null) {
			ArgumentNullException.ThrowIfNull(builder);

			var options = new SoftDeleteOptions();
			configure?.Invoke(options);
			builder.Services.TryAddSingleton(options);
			return builder;
		}

		/// <summary>
		/// Registers soft-delete configuration for a specific repository
		/// registration, enabling any future options knobs to be resolved
		/// from the service provider.
		/// </summary>
		/// <param name="builder">
		/// The per-entity repository builder to configure.
		/// </param>
		/// <param name="configure">
		/// An optional delegate to configure the <see cref="SoftDeleteOptions"/>.
		/// </param>
		/// <returns>
		/// Returns the same builder for chaining.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Soft-delete filtering activates automatically for any entity
		/// implementing <see cref="ISoftDeletable"/>: this call is not
		/// required to enable filtering.
		/// </para>
		/// </remarks>
		public static RepositoryBuilder WithSoftDelete(this RepositoryBuilder builder, Action<SoftDeleteOptions>? configure = null) {
			ArgumentNullException.ThrowIfNull(builder);

			var options = new SoftDeleteOptions();
			configure?.Invoke(options);
			builder.Services.TryAddSingleton(options);
			return builder;
		}
	}
}