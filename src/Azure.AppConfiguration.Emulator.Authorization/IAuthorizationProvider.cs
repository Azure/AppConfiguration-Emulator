using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AppConfig.Service.Authorization
{
    public interface IAuthorizationProvider
    {
        ValueTask<RbacResult> CheckAccess(
            ClaimsPrincipal principal,
            IEnumerable<string> actions,
            CancellationToken cancellationToken);
    }
}
