// Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MongoDB.Bson;

using MongoFramework.Attributes;

namespace Kista {
	[Table("soft_deletable_persons")]
	public class SoftDeletableMongoPerson : IPerson<ObjectId>, IHaveTimeStamp, ISoftDeletable {
		[Key, Column("_id")]
		public ObjectId Id { get; set; }

		[Column("first_name")]
		public string FirstName { get; set; }

		[Column("last_name")]
		public string LastName { get; set; }

		[Column("birthdate")]
		public DateTime? DateOfBirth { get; set; }

		[Column("email")]
		public string? Email { get; set; }

		[Column("phone")]
		public string? PhoneNumber { get; set; }

		[Column("created_at")]
		public DateTimeOffset? CreatedAtUtc { get; set; }

		[Column("updated_at")]
		public DateTimeOffset? UpdatedAtUtc { get; set; }

		[Column("is_deleted")]
		public bool IsDeleted { get; set; }

		[Column("deleted_at")]
		public DateTimeOffset? DeletedAtUtc { get; set; }

		[Column("deleted_by")]
		public string? DeletedBy { get; set; }

		[Column("relationships")]
		public List<MongoPersonRelationship>? Relationships { get; set; }

		IEnumerable<IRelationship> IPerson<ObjectId>.Relationships => Relationships ?? Enumerable.Empty<IRelationship>();
	}
}