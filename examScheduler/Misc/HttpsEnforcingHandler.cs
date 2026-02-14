namespace examScheduler.Misc;

public class HttpsEnforcingHandler : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => request.RequestUri?.Scheme != Uri.UriSchemeHttps
			? throw new InvalidOperationException("HTTPS required")
			: base.SendAsync(request, cancellationToken);

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => request.RequestUri?.Scheme != Uri.UriSchemeHttps
			? throw new InvalidOperationException("HTTPS required")
			: base.Send(request, cancellationToken);
}
