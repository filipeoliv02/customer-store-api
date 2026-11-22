using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerStoreApi.Tests.Fakes;

/// <summary>
/// Defines an <see cref="HttpMessageHandler"/> that records requests and returns a configurable response.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubHttpMessageHandler"/> class.
    /// </summary>
    /// <param name="responder">The function that produces the response for each request.</param>
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        this.responder = responder;
    }

    /// <summary>
    /// Gets the URIs of the requests that were sent.
    /// </summary>
    public IList<Uri?> RequestUris { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        this.RequestUris.Add(request.RequestUri);
        return Task.FromResult(this.responder(request));
    }
}
