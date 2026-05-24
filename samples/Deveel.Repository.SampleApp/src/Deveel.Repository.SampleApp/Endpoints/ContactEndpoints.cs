using Deveel.Data;
using Deveel.Repository.SampleApp.DTOs;
using Deveel.Repository.SampleApp.Models;
using Deveel.Repository.SampleApp.Repositories;

namespace Deveel.Repository.SampleApp.Endpoints;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contacts")
            .WithTags("Contacts")
            .WithOpenApi();

        group.MapGet("/", async (
            IRepository<Contact, Guid> repository,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            if (page.HasValue && pageSize.HasValue)
            {
                var result = await repository.GetPageAsync(page.Value, pageSize.Value, ct);
                var contacts = (result.Items ?? Array.Empty<Contact>()).Select(ToResponse);
                return Results.Ok(new
                {
                    Items = contacts,
                    TotalItems = result.TotalItems,
                    Page = result.Request.Page,
                    PageSize = result.Request.Size,
                    TotalPages = result.TotalPages
                });
            }

            var allContacts = (await repository.FindAllAsync(ct) ?? Array.Empty<Contact>()).Select(ToResponse);
            return Results.Ok(allContacts);
        })
        .WithName("GetAllContacts")
        .WithSummary("Get all contacts with optional pagination")
        .WithDescription("Retrieves a list of all contacts. Supports pagination via query parameters.");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IRepository<Contact, Guid> repository,
            CancellationToken ct) =>
        {
            var contact = await repository.FindAsync(id, ct);
            return contact is not null
                ? Results.Ok(ToResponse(contact))
                : Results.NotFound();
        })
        .WithName("GetContactById")
        .WithSummary("Get a contact by ID")
        .WithDescription("Retrieves a single contact by their unique identifier.");

        group.MapPost("/", async (
            CreateContactRequest request,
            IRepository<Contact, Guid> repository,
            CancellationToken ct) =>
        {
            var contact = new Contact
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone
            };

            await repository.AddAsync(contact, ct);

            return Results.Created($"/api/contacts/{contact.Id}", ToResponse(contact));
        })
        .WithName("CreateContact")
        .WithSummary("Create a new contact")
        .WithDescription("Creates a new contact and returns it with the generated ID.");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateContactRequest request,
            IRepository<Contact, Guid> repository,
            CancellationToken ct) =>
        {
            var contact = await repository.FindAsync(id, ct);
            if (contact is null)
            {
                return Results.NotFound();
            }

            contact.FirstName = request.FirstName;
            contact.LastName = request.LastName;
            contact.Email = request.Email;
            contact.Phone = request.Phone;
            contact.UpdatedAt = DateTime.UtcNow;

            await repository.UpdateAsync(contact, ct);

            return Results.Ok(ToResponse(contact));
        })
        .WithName("UpdateContact")
        .WithSummary("Update an existing contact")
        .WithDescription("Updates a contact's information and returns the updated entity.");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IRepository<Contact, Guid> repository,
            CancellationToken ct) =>
        {
            var contact = await repository.FindAsync(id, ct);
            if (contact is null)
            {
                return Results.NotFound();
            }

            await repository.RemoveAsync(contact, ct);

            return Results.NoContent();
        })
        .WithName("DeleteContact")
        .WithSummary("Delete a contact")
        .WithDescription("Removes a contact from the repository.");

        group.MapDelete("/", async (
            IRepository<Contact, Guid> repository,
            CancellationToken ct) =>
        {
            var contacts = await repository.FindAllAsync(ct);
            if (contacts != null)
            {
                await repository.RemoveRangeAsync(contacts, ct);
            }

            return Results.NoContent();
        })
        .WithName("DeleteAllContacts")
        .WithSummary("Delete all contacts")
        .WithDescription("Removes all contacts from the repository.");
    }

    private static ContactResponse ToResponse(Contact contact) =>
        new(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.Email,
            contact.Phone,
            contact.CreatedAt,
            contact.UpdatedAt
        );
}
