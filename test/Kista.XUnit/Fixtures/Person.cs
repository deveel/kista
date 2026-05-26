using System.ComponentModel.DataAnnotations;

namespace Kista;

/// <summary>
/// A test entity representing a person, used across all unit and integration
/// tests in the <c>Kista.XUnit</c> project.
/// The <see cref="Id"/> property is decorated with <see cref="KeyAttribute"/>,
/// enabling key discovery by <see cref="RepositoryWrapper{TEntity}"/>.
/// </summary>
public class Person
{
    /// <summary>
    /// Gets or sets the unique identifier of the person.
    /// </summary>
    [Key]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the first name of the person.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name of the person.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of birth of the person.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the email address of the person.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the person.
    /// </summary>
    public string? Phone { get; set; }
}
