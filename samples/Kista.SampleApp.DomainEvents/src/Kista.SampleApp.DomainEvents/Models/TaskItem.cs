using System.ComponentModel.DataAnnotations;

using Kista;

namespace Kista.SampleApp.DomainEvents;

public class TaskItem {
	[Key]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Required]
	public string Title { get; set; } = string.Empty;

	public string? Description { get; set; }

	public bool IsCompleted { get; set; }

	public DateTimeOffset? CreatedAtUtc { get; set; }

	public DateTimeOffset? UpdatedAtUtc { get; set; }
}