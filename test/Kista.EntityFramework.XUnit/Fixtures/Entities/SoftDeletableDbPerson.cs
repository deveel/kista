// Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kista.Entities {
	[Table("soft_deletable_people")]
	public class SoftDeletableDbPerson : IPerson<Guid>, ISoftDeletable {
		[Key]
		public Guid Id { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string? Email { get; set; }

		public DateTime? DateOfBirth { get; set; }

		public string? PhoneNumber { get; set; }

		public DateTimeOffset? CreatedAtUtc { get; set; }

		public DateTimeOffset? UpdatedAtUtc { get; set; }

		public bool IsDeleted { get; set; }

		public DateTimeOffset? DeletedAtUtc { get; set; }

		public string? DeletedBy { get; set; }

		public virtual List<SoftDeletableDbRelationship>? Relationships { get; set; }

		IEnumerable<IRelationship> IPerson<Guid>.Relationships
			=> Relationships ?? Enumerable.Empty<IRelationship>();
	}
}