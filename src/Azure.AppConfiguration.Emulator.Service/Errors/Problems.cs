using Azure.AppConfiguration.Emulator.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class Problems
    {
        public static readonly ProblemDetails InvalidState = new ProblemDetails
        {
            Type = ErrorType.InvalidState,
            Title = "Target resource state invalid.",
            Detail = "The target resource is not in a valid state to perform the requested operation.",
            Status = StatusCodes.Status409Conflict
        };

        public static readonly ProblemDetails AlreadyExists = new ProblemDetails
        {
            Type = ErrorType.AlreadyExists,
            Title = "The resource already exists.",
            Detail = string.Empty,
            Status = StatusCodes.Status409Conflict
        };
    }
}
