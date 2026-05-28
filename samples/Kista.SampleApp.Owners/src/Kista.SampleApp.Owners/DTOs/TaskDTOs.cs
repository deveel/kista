namespace Kista.SampleApp.Owners.DTOs;

public record CreateTaskRequest(
    string Title
);

public record UpdateTaskRequest(
    string Title,
    bool IsCompleted
);

public record TaskResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    string Owner,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
