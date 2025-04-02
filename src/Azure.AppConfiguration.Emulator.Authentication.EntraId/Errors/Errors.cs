// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    internal class Errors
    {
        public static readonly AuthenticationError InvalidToken = new AuthenticationError { Code = "InvalidToken", Message = "Authorization token failed validation" };
        public static readonly AuthenticationError InvalidTokenTenant = new AuthenticationError { Code = "InvalidAuthenticationTokenTenant", Message = "The access token is from the wrong issuer. It must match the AD tenant associated with the subscription, to which the configuration store belongs. If you just transferred your subscription and see this error message, please try back later." };
        public static readonly AuthenticationError InvalidAuthenticationToken = new AuthenticationError { Code = "InvalidAuthenticationToken", Message = "The received access token is not valid. If you are accessing as application please make sure service principal is properly created in the tenant." };
        public static readonly AuthenticationError AuthorizationFailed = new AuthenticationError { Code = "AuthorizationFailed", Message = "The received access token does not have authorization to perform action over the requested scope or the scope is invalid. If access was recently granted, please refresh your credentials." };
        public static readonly AuthenticationError ResourceNotFound = new AuthenticationError { Code = "ResourceNotFound", Message = "The configuration store resource is not found" };
        public static readonly AuthenticationError ResourceGroupNotFound = new AuthenticationError { Code = "ResourceGroupNotFound", Message = "The resource group is not found" };
        public static readonly AuthenticationError SubscriptionNotFound = new AuthenticationError { Code = "SubscriptionNotFound", Message = "The subscription is not found" };

        // See https://docs.microsoft.com/en-us/azure/active-directory/develop/reference-aadsts-error-codes
        public static readonly AuthenticationError NationalCloudTenantRedirection = new AuthenticationError { Code = "90038", Message = "Tenant belongs to a cloud that doesn't federate the current cloud instance" };
        public static readonly AuthenticationError ConfidentialClient = new AuthenticationError { Code = "900382", Message = "Confidential Client is not supported in Cross Cloud request" };
        public static readonly AuthenticationError UnsupportedTenantHost = new AuthenticationError { Code = "900440", Message = "Tenant hosted in the public cloud is not supported" };
        public static readonly AuthenticationError TenantFederationError = new AuthenticationError { Code = "900381", Message = " Tenant belongs to a cloud that doesn't federate the current cloud instance" };
    }
}
