using Kista;
using Kista.SampleApp.Owners.DTOs;
using Kista.SampleApp.Owners.Models;

namespace Kista.SampleApp.Owners.Endpoints;

public static class NoteEndpoints
{
    public static void MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notes")
            .WithTags("Notes")
            .WithOpenApi();

        group.MapGet("/", async (
            IRepository<Note, Guid> repository,
            CancellationToken ct) =>
        {
            var notes = await repository.FindAllAsync(ct);
            return Results.Ok((notes ?? Array.Empty<Note>()).Select(ToResponse));
        })
        .WithName("GetAllNotes")
        .WithSummary("Get all notes for the current user");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IRepository<Note, Guid> repository,
            CancellationToken ct) =>
        {
            var note = await repository.FindAsync(id, ct);
            return note is not null
                ? Results.Ok(ToResponse(note))
                : Results.NotFound();
        })
        .WithName("GetNoteById")
        .WithSummary("Get a note by ID");

        group.MapPost("/", async (
            CreateNoteRequest request,
            IRepository<Note, Guid> repository,
            CancellationToken ct) =>
        {
            var note = new Note
            {
                Title = request.Title,
                Content = request.Content
            };

            await repository.AddAsync(note, ct);

            return Results.Created($"/api/notes/{note.Id}", ToResponse(note));
        })
        .WithName("CreateNote")
        .WithSummary("Create a new note")
        .WithDescription("Owner is automatically set from the current user context.");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateNoteRequest request,
            IRepository<Note, Guid> repository,
            CancellationToken ct) =>
        {
            var note = await repository.FindAsync(id, ct);
            if (note is null)
                return Results.NotFound();

            note.Title = request.Title;
            note.Content = request.Content;
            note.UpdatedAt = DateTime.UtcNow;

            await repository.UpdateAsync(note, ct);

            return Results.Ok(ToResponse(note));
        })
        .WithName("UpdateNote")
        .WithSummary("Update an existing note");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IRepository<Note, Guid> repository,
            CancellationToken ct) =>
        {
            var note = await repository.FindAsync(id, ct);
            if (note is null)
                return Results.NotFound();

            await repository.RemoveAsync(note, ct);

            return Results.NoContent();
        })
        .WithName("DeleteNote")
        .WithSummary("Delete a note");

        group.MapDelete("/", async (
            IRepository<Note, Guid> repository,
            CancellationToken ct) =>
        {
            var notes = await repository.FindAllAsync(ct);
            if (notes != null)
                await repository.RemoveRangeAsync(notes, ct);

            return Results.NoContent();
        })
        .WithName("DeleteAllNotes")
        .WithSummary("Delete all notes for the current user");
    }

    private static NoteResponse ToResponse(Note note) =>
        new(
            note.Id,
            note.Title,
            note.Content,
            note.OwnerId,
            note.CreatedAt,
            note.UpdatedAt
        );
}
