using FluentValidation.Results;

namespace SanShine.SaaS.Core.Common.Core.Extensions.CustomExceptions;

public class ValidationErrorDetails : ErrorDetails
{
    public IEnumerable<ValidationFailure> Errors { get; set; }
}
