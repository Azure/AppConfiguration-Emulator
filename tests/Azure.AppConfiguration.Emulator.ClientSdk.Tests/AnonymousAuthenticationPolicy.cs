using Azure.Core;
using Azure.Core.Pipeline;

namespace Azure.AppConfiguration.Emulator.ClientSdk.Tests
{
    internal sealed class AnonymousAuthenticationPolicy : HttpPipelinePolicy
    {
        private const string AuthorizationHeaderName = "Authorization";

        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            message.Request.Headers.Remove(AuthorizationHeaderName);
            ProcessNext(message, pipeline);
        }

        public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            message.Request.Headers.Remove(AuthorizationHeaderName);
            return ProcessNextAsync(message, pipeline);
        }
    }

    internal sealed class AnonymousTokenCredential : TokenCredential
    {
        private static readonly AccessToken AccessToken = new(nameof(AnonymousTokenCredential), DateTimeOffset.MaxValue);

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return AccessToken;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(AccessToken);
        }
    }
}