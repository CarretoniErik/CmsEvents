using CmsEvents.Application.UseCases.ProcessCmsEvents;
using FluentValidation;

namespace CmsEvents.UnitTests.Infrastructure;

public sealed class AcceptAllValidator : AbstractValidator<ProcessCmsEventsInput> { }