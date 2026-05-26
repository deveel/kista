// Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kista.Entities {
	[Table("tenant_people")]
	public class DbTenantPerson : IPerson<Guid> {
		[Key]
		public Guid Id { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string? Email { get; set; }

		public string? PhoneNumber { get; set; }

		public DateTime? DateOfBirth { get; set; }

		public string TenantId { get; set; }

		public DateTimeOffset? CreatedAtUtc { get; set; }

		public DateTimeOffset? UpdatedAtUtc { get; set; }

		public virtual List<DbTenantPersonRelationship> Relationships { get; set; }

		IEnumerable<IRelationship> IPerson<Guid>.Relationships => Relationships;
	}

	[Table("tenant_person_relationships")]	
	public class DbTenantPersonRelationship : IRelationship {
		[Key]
		public Guid Id { get; set; }

		public Guid? PersonId { get; set; }

		public virtual DbTenantPerson Person { get; set; }

		public string Type { get; set; }

		public string FullName { get; set; }

	}
}
