using System.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.FeatureManagement;

namespace PollyWithHttpClient;

public class DistributedCachingHandler(IDistributedCache distributedCache, IHttpContextAccessor accessor, IFeatureManagerSnapshot featureManagerSnapshot) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {

        // using featureManagerSnapshot dependency sets the feature value captured for all requests coming in
        // within lifetime of the http delegating handler which is usually 5 minutes

        // retrieve the service from the Request DI Scope
        var featureManagerFromHttpContext = accessor.HttpContext?.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

        var isEnabledAsync = featureManagerFromHttpContext is not null && await featureManagerFromHttpContext.IsEnabledAsync("DistributedCache");
        if (!isEnabledAsync)
        {
            Console.WriteLine("Feature Off");
            return await base.SendAsync(request, cancellationToken);
        }

        Console.WriteLine("FeatureOn");
        var cachedValue = await GetValue(request, cancellationToken);
        if (cachedValue is not null)
            return cachedValue;
        var response = await base.SendAsync(request, cancellationToken);

        await SetCacheAsync(request, response, cancellationToken);

        return response;
    }

    private async Task<HttpResponseMessage?> GetValue(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri == null) return null;
        var cachedValue = await GetAsync(request, cancellationToken);
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        return null;
    }

    private async Task<HttpResponseMessage?> GetAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = await distributedCache.GetAsync(request.RequestUri.ToString(), cancellationToken);

        if (content == null) return null;
        {
            return new HttpResponseMessage( HttpStatusCode.OK ) { Content =  new ByteArrayContent(content) };
        }
    }

    private async Task SetCacheAsync(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.OK)
            return;
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            //TODO: set an expiration:
        if (request.RequestUri != null)
            await distributedCache.SetAsync(request.RequestUri.ToString(), content, cancellationToken);
    }
}
