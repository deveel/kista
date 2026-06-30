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
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kista {
	/// <summary>
	/// An <see cref="IEntityTypeConfiguration{TEntity}"/> that applies the
	/// soft-delete global query filter to an entity implementing
	/// <see cref="ISoftDeletable"/>.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to configure.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// Register this configuration in your <c>OnModelCreating</c> override
	/// through <c>modelBuilder.ApplyConfiguration(new SoftDeleteEntityTypeConfiguration&lt;TEntity&gt;())</c>
	/// for explicit per-type registration, or use
	/// <see cref="SoftDeleteModelBuilderExtensions.HasSoftDeleteFilter(ModelBuilder)"/>
	/// for a single convention-style call covering all soft-deletable entities.
	/// </para>
	/// </remarks>
	public class SoftDeleteEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
		where TEntity : class, ISoftDeletable {
		/// <inheritdoc />
		public void Configure(EntityTypeBuilder<TEntity> builder) {
			ArgumentNullException.ThrowIfNull(builder);

			builder.HasSoftDeleteFilter();
		}
	}
}