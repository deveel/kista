// Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;

namespace Kista {
	public class Person : IPerson {
		[Key]
		public string? Id { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set;}

		public DateTime? DateOfBirth { get; set; }

		public string? Email { get; set; }

		public string? PhoneNumber { get; set; }

		public DateTimeOffset? CreatedAtUtc { get; set; }

		public DateTimeOffset? UpdatedAtUtc { get; set; }

		public List<PersonRelationship> Relationships { get; set; } = new List<PersonRelationship>();

		IEnumerable<IRelationship> IPerson<string>.Relationships => Relationships;
	}
}
