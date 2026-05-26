// Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using NetTopologySuite.Geometries;

namespace Kista.Entities {
	[Table("people")]
	public class DbPerson : IPerson<Guid> {
		[Key]
		public Guid Id { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string? Description { get; set; }

		public Point? Location { get; set; }

		public virtual List<DbRelationship>? Relationships { get; set; }

		IEnumerable<IRelationship> IPerson<Guid>.Relationships
			=> Relationships ?? Enumerable.Empty<IRelationship>();

		public string? Email { get; set; }

		public DateTime? DateOfBirth { get; set; }

		public string? PhoneNumber { get; set; }

		public DateTimeOffset? CreatedAtUtc { get; set; }

		public DateTimeOffset? UpdatedAtUtc { get; set; }
	}

	[Table("person_relationships")]
	public class DbRelationship : IRelationship {
		[Key]
		public Guid Id { get; set; }

		public Guid? PersonId { get; set; }

		public virtual DbPerson Person { get; set; }

		public string Type { get; set; }

		public string FullName { get; set; }
	}
}
