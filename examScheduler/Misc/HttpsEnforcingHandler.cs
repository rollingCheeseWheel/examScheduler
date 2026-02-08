namespace examScheduler.Misc;

public class HttpsEnforcingHandler : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri?.Scheme != Uri.UriSchemeHttps )
		{
			throw new InvalidOperationException("HTTPS required");
		}
		return base.SendAsync(request, cancellationToken);
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri?.Scheme != Uri.UriSchemeHttps)
		{
			throw new InvalidOperationException("HTTPS required");
		}
		return base.Send(request, cancellationToken);
	}
}
