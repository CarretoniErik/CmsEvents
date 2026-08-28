using CmsEvents.IntegrationTests.Fixtures;
using CmsEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace CmsEvents.IntegrationTests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public sealed class DisableCmsEventEndpointTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ShouldDisableCmsEventWhenAdminHasValidCredentials()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.CmsUsername, TestCredentials.CmsPassword);
        const string eventId = "entity-1";
        var events = new[]
        {
            new
            {
                type = "publish",
                id = eventId,
                payload = new { name = "Test entity" },
                version = 1,
                timestamp = DateTimeOffset.UtcNow
            }
        };

        var postResponse = await client.PostCmsEventsAsync(events);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        client.SetCredentials(TestCredentials.AdminUsername, TestCredentials.AdminPassword);

        // Act
        var response = await client.DeleteCmsEventAsync(eventId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDisableEventDoesNotExist()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.AdminUsername, TestCredentials.AdminPassword);
        const string eventId = "non-existent-event";

        // Act
        var response = await client.DeleteCmsEventAsync(eventId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldReturnForbiddenWhenUserTriesToDisableCmsEvent()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.ReaderUsername, TestCredentials.ReaderPassword);
        const string eventId = "entity-1";

        // Act
        var response = await client.DeleteCmsEventAsync(eventId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}