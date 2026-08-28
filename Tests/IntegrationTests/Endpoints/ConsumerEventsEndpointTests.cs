using CmsEvents.Application.UseCases.ListCmsEvents;
using CmsEvents.IntegrationTests.Fixtures;
using CmsEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace CmsEvents.IntegrationTests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public sealed class ConsumerEventsEndpointTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ShouldReturnCmsEventsWhenConsumerHasValidCredentials()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.CmsUsername, TestCredentials.CmsPassword);
        var events = new[]
        {
            CreateEvent("entity-1", 1),
            CreateEvent("entity-2", 2)
        };
        var postResponse = await client.PostCmsEventsAsync(events);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        client.SetCredentials(TestCredentials.ReaderUsername, TestCredentials.ReaderPassword);

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var receivedEvents = await response.Content.ReadFromJsonAsync<List<CmsEventListItem>>();
        receivedEvents.Should().NotBeNull();
        receivedEvents.Should().ContainSingle(e => e.Id == "entity-1" && e.Version == 1);
        receivedEvents.Should().ContainSingle(e => e.Id == "entity-2" && e.Version == 2);
    }

    [Fact]
    public async Task ShouldRequestOnlyVisibleEventsWhenConsumerIsNotAdmin()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.CmsUsername, TestCredentials.CmsPassword);
        var events = new[]
        {
            CreateEvent("visible-entity", 1),
            CreateEvent("disabled-entity", 1)
        };

        var postResponse = await client.PostCmsEventsAsync(events);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        client.SetCredentials(TestCredentials.AdminUsername, TestCredentials.AdminPassword);
        var deleteResponse = await client.DeleteCmsEventAsync("disabled-entity");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        client.SetCredentials(TestCredentials.ReaderUsername, TestCredentials.ReaderPassword);

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var receivedEvents = await response.Content.ReadFromJsonAsync<List<CmsEventListItem>>();
        receivedEvents.Should().NotBeNull();
        receivedEvents.Should().ContainSingle(e => e.Id == "visible-entity");
        receivedEvents.Should().NotContain(e => e.Id == "disabled-entity");
    }

    private static object CreateEvent(string id, int version)
    {
        return new
        {
            type = "publish",
            id,
            payload = new { name = id },
            version,
            timestamp = DateTimeOffset.UtcNow
        };
    }
}