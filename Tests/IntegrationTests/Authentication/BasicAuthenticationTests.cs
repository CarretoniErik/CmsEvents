using CmsEvents.IntegrationTests.Fixtures;
using CmsEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace CmsEvents.IntegrationTests.Authentication;

[Collection(IntegrationTestCollection.Name)]
public sealed class BasicAuthenticationTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ShouldAllowUserWithValidCredentials()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.ReaderUsername, TestCredentials.ReaderPassword);

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldRejectUserWithInvalidUsername()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials("unknown-user", TestCredentials.ReaderPassword);

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldRejectUserWithInvalidPassword()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.ReaderUsername, "wrong-secret");

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldRejectRequestWithoutCredentials()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());

        // Act
        var response = await client.GetCmsEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldAllowCmsWithValidCredentials()
    {
        // Arrange
        using var client = new CmsEventsApiClient(Fixture.ApiFactory.CreateClient());
        client.SetCredentials(TestCredentials.CmsUsername, TestCredentials.CmsPassword);

        // Act
        var response = await client.PostCmsEventsAsync(Array.Empty<object>());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}