namespace Kista.SampleApp.Owners.DTOs;

public record CreateNoteRequest(
    string Title,
    string Content
);

public record UpdateNoteRequest(
    string Title,
    string Content
);

public record NoteResponse(
    Guid Id,
    string Title,
    string Content,
    string OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
