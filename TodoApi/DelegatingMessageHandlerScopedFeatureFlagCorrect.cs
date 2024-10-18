using Microsoft.FeatureManagement;

namespace TodoApi;

public class DelegatingMessageHandlerScopedFeatureFlagCorrect(
    IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {

        Console.WriteLine($"Scoped Guid in delegating handler {accessor.HttpContext?.RequestServices.GetRequiredService<ScopedClass>().GuidString.ToString()}");

        var featureManagerFromHttpContext = accessor.HttpContext?.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();
        var isEnabledAsync = featureManagerFromHttpContext is not null &&
                             await featureManagerFromHttpContext.IsEnabledAsync("Feature");


        if (isEnabledAsync)
        {
            Console.WriteLine("Feature Off");
            return await base.SendAsync(request, cancellationToken);
        }

        Console.WriteLine("FeatureOn");
        return await base.SendAsync(request, cancellationToken);
    }
}
