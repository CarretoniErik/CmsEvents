using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace CmsEvents.IntegrationTests.Infrastructure;

public sealed class CmsEventsApiClient(HttpClient client) : IDisposable
{
    private readonly HttpClient client = client;

    //public CmsEventsApiClient()
    //{
    //    client = new HttpClient
    //    {
    //        BaseAddress = new Uri(Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:8080")
    //    };
    //}

    public Task<HttpResponseMessage> PostCmsEventsAsync(object events, CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonAsync("/cms/events", events, cancellationToken);
    }

    public Task<HttpResponseMessage> GetCmsEventsAsync(CancellationToken cancellationToken = default)
    {
        return client.GetAsync("/consumers/cms/events", cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteCmsEventAsync(string id, CancellationToken cancellationToken = default)
    {
        return client.DeleteAsync($"/consumers/cms/events/{id}", cancellationToken);
    }

    public void SetCredentials(string username, string password)
    {        
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public void Dispose() => client.Dispose();
}
