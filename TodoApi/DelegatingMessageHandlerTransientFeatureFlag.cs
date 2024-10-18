using Microsoft.FeatureManagement;

namespace TodoApi;

public class DelegatingMessageHandlerTransientFeatureFlag(
    IFeatureManager featureManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {

        if (!await featureManager.IsEnabledAsync("Feature"))
        {
            Console.WriteLine("Feature Off");
            return await base.SendAsync(request, cancellationToken);
        }

        Console.WriteLine("FeatureOn");
        return await base.SendAsync(request, cancellationToken);
    }
}
