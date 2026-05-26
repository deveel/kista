using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using NetTopologySuite.Geometries;

namespace Kista {
	/// <summary>
	/// An EF Core model customizer that removes geometry-typed properties from the
	/// compiled model when SpatiaLite is not available on the current platform.
	/// This allows non-spatial tests to run on machines that do not have the
	/// SpatiaLite native library installed (e.g. macOS without libspatialite).
	/// </summary>
	internal sealed class NonSpatialModelCustomizer(ModelCustomizerDependencies dependencies)
		: ModelCustomizer(dependencies) {

		public override void Customize(ModelBuilder modelBuilder, DbContext context) {
			// ── Phase 1 (PRE-onModelCreating) ──────────────────────────────────────
			// Ignore geometry properties on every DbSet-backed entity type before
			// OnModelCreating runs.  This prevents EF Core's convention scanner from
			// discovering NetTopologySuite types (Point, Geometry, …) as entity / owned
			// types, which would later cause "Point.UserData could not be mapped" errors.
			var dbSetEntityTypes = context.GetType()
				.GetProperties()
				.Where(p => p.PropertyType.IsGenericType &&
				            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
				.Select(p => p.PropertyType.GetGenericArguments()[0]);

			foreach (var entityClrType in dbSetEntityTypes) {
				foreach (var prop in entityClrType.GetProperties()) {
					var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
					if (typeof(Geometry).IsAssignableFrom(propType))
						modelBuilder.Entity(entityClrType).Ignore(prop.Name);
				}
			}

			// Run the standard customization (calls context.OnModelCreating)
			base.Customize(modelBuilder, context);

			// ── Phase 2 (POST-onModelCreating) ─────────────────────────────────────
			// Remove any NTS geometry types that conventions may still have added as
			// entity / complex types (e.g. because they were reachable via navigation
			// properties not covered by Phase 1).
			var geometryEntityTypes = modelBuilder.Model
				.GetEntityTypes()
				.Where(et => typeof(Geometry).IsAssignableFrom(et.ClrType))
				.Select(et => et.ClrType)
				.ToList();

			foreach (var geoType in geometryEntityTypes)
				modelBuilder.Ignore(geoType);

			// Strip any surviving geometry-typed properties from mapped entity types.
			foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList()) {
				var geometryProps = entityType
					.GetProperties()
					.Where(p => typeof(Geometry).IsAssignableFrom(p.ClrType))
					.Select(p => p.Name)
					.ToList();

				foreach (var propName in geometryProps)
					modelBuilder.Entity(entityType.ClrType).Ignore(propName);
			}
		}
	}
}


