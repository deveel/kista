// Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kista.Entities {
	[Table("soft_deletable_person_relationships")]
	public class SoftDeletableDbRelationship : IRelationship {
		[Key]
		public Guid Id { get; set; }

		public Guid? PersonId { get; set; }

		public virtual SoftDeletableDbPerson? Person { get; set; }

		public string Type { get; set; }

		public string FullName { get; set; }
	}
}