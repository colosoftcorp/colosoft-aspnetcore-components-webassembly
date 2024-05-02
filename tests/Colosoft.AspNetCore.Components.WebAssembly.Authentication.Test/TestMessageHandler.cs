namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal class TestMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public TestMessageHandler(HttpResponseMessage response) => this.response = response;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.response.RequestMessage = request;
        return Task.FromResult(this.response);
    }
}
