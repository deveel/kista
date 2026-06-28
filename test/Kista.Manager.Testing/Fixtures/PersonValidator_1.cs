using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace Kista {
	public class PersonValidator<TPerson> : IEntityValidator<TPerson>
		where TPerson : class, IPerson {
		public async IAsyncEnumerable<ValidationResult> ValidateAsync(EntityManager<TPerson> manager, TPerson entity, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
			if (entity.Email != null && !MailAddress.TryCreate(entity.Email, out var _))
				yield return new ValidationResult("The email address is not valid", new[] { nameof(IPerson.Email) });

			await Task.CompletedTask;
		}
	}
}
