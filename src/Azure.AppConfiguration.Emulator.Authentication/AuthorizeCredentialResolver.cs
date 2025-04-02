using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Authentication
{
    //
    // Authorization Token Parser
    // 
    // HTTP Request
    // Authorize: <scheme> <token>
    //
    public class AuthorizeCredentialResolver : ICredentialResolver
    {
        private readonly IHttpContextAccessor _http;

        public AuthorizeCredentialResolver(IHttpContextAccessor http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public Credential GetCredential()
        {
            HttpContext context = _http.HttpContext;

            if (context == null)
            {
                return null;
            }

            StringValues authHeader;
            StringValues hostHeader;

            if (!context.Request.Headers.TryGetValue(HeaderNames.Host, out hostHeader))
            {
                return null;
            }

            var credential = new Credential
            {
                Host = hostHeader.First()
            };

            if (!context.Request.Headers.TryGetValue(HeaderNames.Authorization, out authHeader))
            {
                //
                // Anonymous
                credential.Scheme = AuthenticationSchemes.Anonymous;

                return credential;
            }

            string auth = authHeader.First();

            //
            // Parse scheme and token
            for (int i = 0; i < auth.Length; ++i)
            {
                if (!char.IsWhiteSpace(auth[i]))
                {
                    int space = auth.IndexOf(' ', i);

                    if (space < 0)
                    {
                        space = auth.Length;
                    }
                    else
                    {
                        credential.Value = auth.Substring(space).Trim();
                    }

                    credential.Scheme = auth.Substring(i, space - i);

                    break;
                }
            }

            if (string.IsNullOrEmpty(credential.Value) ||
                string.IsNullOrEmpty(credential.Scheme))
            {
                return null;
            }

            return credential;
        }
    }
}
