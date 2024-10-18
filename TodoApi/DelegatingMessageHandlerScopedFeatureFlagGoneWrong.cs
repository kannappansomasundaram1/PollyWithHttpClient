using Microsoft.FeatureManagement;

namespace TodoApi;

public class DelegatingMessageHandlerScopedFeatureFlagGoneWrong(
    IFeatureManagerSnapshot featureManagerSnapshot,
    ScopedClass scopedClass) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {

        Console.WriteLine($"Scoped Guid in delegating handler {scopedClass.GuidString.ToString()}");

        if (!await featureManagerSnapshot.IsEnabledAsync("Feature"))
        {
            Console.WriteLine("Feature Off");
            return await base.SendAsync(request, cancellationToken);
        }

        Console.WriteLine("FeatureOn");
        return await base.SendAsync(request, cancellationToken);
    }
}
