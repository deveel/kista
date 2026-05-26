// Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations.Schema;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using MongoFramework;

namespace Kista {
	[MultiTenant, Table("persons")]
	public class MongoTenantPerson : MongoPerson, IHaveTenantId, IHaveTimeStamp {
		[Column("tenant")]
		public string TenantId { get; set; }
	}
}
