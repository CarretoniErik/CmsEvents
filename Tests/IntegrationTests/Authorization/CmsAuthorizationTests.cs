using CmsEvents.IntegrationTests.Fixtures;
using CmsEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace CmsEvents.IntegrationTests.Authorization;

[Collection(IntegrationTestCollection.Name)]
public sealed class CmsAuthorizationTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ShouldRejectUserFromCmsEndpoint()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.ReaderUsername, TestCredentials.ReaderPassword);

        // Act
        var response = await client.PostCmsEventsAsync(Array.Empty<object>());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldRejectAdminFromCmsEndpoint()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.AdminUsername, TestCredentials.AdminPassword);

        // Act
        var response = await client.PostCmsEventsAsync(Array.Empty<object>());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}