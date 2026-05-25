using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace Kista {
	public class PersonValidator<TPerson> : IEntityValidator<TPerson>
		where TPerson : class, IPerson {
		public async IAsyncEnumerable<ValidationResult> ValidateAsync(EntityManager<TPerson> manager, TPerson person, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
			if (person.Email != null && !MailAddress.TryCreate(person.Email, out var _))
				yield return new ValidationResult("The email address is not valid", new[] { nameof(IPerson.Email) });

			await Task.CompletedTask;
		}
	}
}
