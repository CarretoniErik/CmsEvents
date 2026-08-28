using CmsEvents.IntegrationTests.Fixtures;
using CmsEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace CmsEvents.IntegrationTests.Authorization;

[Collection(IntegrationTestCollection.Name)]
public sealed class ConsumerAuthorizationTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ShouldRejectCmsUserFromConsumerEndpoint()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.CmsUsername, TestCredentials.CmsPassword);

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldAllowAdminUserFromConsumerEndpoint()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.AdminUsername, TestCredentials.AdminPassword);

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldRejectUserFromAdminEndpoint()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.ReaderUsername, TestCredentials.ReaderPassword);

        // Act
        var response = await client.DeleteCmsEventAsync("some-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldAllowAdminFromAdminEndpoint()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        var request = new[]
        {
            new
            {
                type = "publish",
                id = "some-id",
                payload = new { name = "Test entity" },
                version = 1,
                timestamp = DateTimeOffset.UtcNow
            }
        };
        client.SetCredentials(TestCredentials.CmsUsername, TestCredentials.CmsPassword);
        var createResponse = await client.PostCmsEventsAsync(request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        client.SetCredentials(TestCredentials.AdminUsername, TestCredentials.AdminPassword);

        // Act
        var response = await client.DeleteCmsEventAsync("some-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}