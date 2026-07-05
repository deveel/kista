namespace Kista.SampleApp.SoftDelete.DTOs;

public record CreateTaskRequest(string Title, bool IsCompleted = false);

public record UpdateTaskRequest(string Title, bool IsCompleted);

public record TaskResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy);