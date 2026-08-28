namespace CmsEvents.Api.Authentication;

public sealed record AuthenticatedUser(string Username, UserRole Role);