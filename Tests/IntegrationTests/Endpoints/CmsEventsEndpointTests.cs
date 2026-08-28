using CmsEvents.IntegrationTests.Fixtures;
using CmsEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CmsEvents.IntegrationTests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public sealed class CmsEventsEndpointTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ShouldProcessCmsEventsWhenCmsHasValidCredentials()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.CmsUsername, TestCredentials.CmsPassword);
        var events = new[]
        {
            new
            {
                type = "publish",
                id = "entity-1",
                payload = new { name = "Test entity" },
                version = 1,
                timestamp = DateTimeOffset.UtcNow
            }
        };

        // Act
        var response = await client.PostCmsEventsAsync(events);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        client.SetCredentials(TestCredentials.ReaderUsername, TestCredentials.ReaderPassword);
        var eventsResponse = await client.GetCmsEventsAsync();
        eventsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var visibleEvents = await eventsResponse.Content.ReadFromJsonAsync<JsonElement>();
        visibleEvents.EnumerateArray().Should().ContainSingle(eventItem => eventItem.GetProperty("id").GetString() == "entity-1");
    }
}