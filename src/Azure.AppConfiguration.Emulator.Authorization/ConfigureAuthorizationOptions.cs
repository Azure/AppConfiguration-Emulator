// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AppConfig.Service.Authorization
{
    class ConfigureAuthorizationOptions : IConfigureOptions<AuthorizationOptions>
    {
        public void Configure(AuthorizationOptions options)
        {
            //
            // Setup policy definitions
            AddDefaultPolicy(options);
            AddKeyValueReadPolicy(options);
            AddKeyValueWritePolicy(options);
            AddKeyValueDeletePolicy(options);
            AddSnapshotReadPolicy(options);
            AddSnapshotCreatePolicy(options);
            AddSnapshotArchivePolicy(options);
        }

        private void AddDefaultPolicy(AuthorizationOptions options)
        {
            const string AuthenticatedUserPolicy = "AuthenticatedUser";

            options.AddPolicy(
                AuthenticatedUserPolicy,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(AuthenticationShemes.All)
                        .RequireClaim(System.Security.Claims.ClaimTypes.Sid));

            options.DefaultPolicy = options.GetPolicy(AuthenticatedUserPolicy);
        }

        private void AddKeyValueReadPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(
                Policies.KeyValueRead,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(AuthenticationShemes.All)
                        .RequireClaim(System.Security.Claims.ClaimTypes.Sid)
                        .RequireAssertion(ctx =>
                        {
                            return ctx.User.IsInRole(Roles.Owner) ||
                            ctx.User.IsInRole(Roles.Reader) ||
                            ctx.User.AllowAction(Actions.KeyValueRead);
                        }));
        }

        private void AddKeyValueWritePolicy(AuthorizationOptions options)
        {
            options.AddPolicy(
                Policies.KeyValueWrite,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(AuthenticationShemes.All)
                        .RequireClaim(System.Security.Claims.ClaimTypes.Sid)
                        .RequireAssertion(ctx =>
                        {
                            return
                                ctx.User.IsInRole(Roles.Owner) ||
                                ctx.User.AllowAction(Actions.KeyValueWrite);
                        }));
        }

        private void AddKeyValueDeletePolicy(AuthorizationOptions options)
        {
            options.AddPolicy(
                Policies.KeyValueDelete,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(AuthenticationShemes.All)
                        .RequireClaim(System.Security.Claims.ClaimTypes.Sid)
                        .RequireAssertion(ctx =>
                                ctx.User.IsInRole(Roles.Owner) ||
                                ctx.User.AllowAction(Actions.KeyValueDelete)));
        }

        private void AddSnapshotReadPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(
                Policies.SnapshotRead,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(AuthenticationShemes.All)
                        .RequireClaim(System.Security.Claims.ClaimTypes.Sid)
                        .RequireAssertion(ctx =>
                            ctx.User.IsInRole(Roles.Owner) ||
                            ctx.User.IsInRole(Roles.Reader) ||
                            ctx.User.AllowAction(Actions.SnapshotRead)));
        }

        private void AddSnapshotCreatePolicy(AuthorizationOptions options)
        {
            options.AddPolicy(
                Policies.SnapshotCreate,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(AuthenticationShemes.All)
                        .RequireClaim(System.Security.Claims.ClaimTypes.Sid)
                        .RequireAssertion(ctx =>
                                ctx.User.IsInRole(Roles.Owner) ||
                                (ctx.User.AllowAction(Actions.SnapshotCreate) && ctx.User.AllowAction(Actions.KeyValueRead))));
        }

        private void AddSnapshotArchivePolicy(AuthorizationOptions options)
        {
            options.AddPolicy(
                Policies.SnapshotArchive,
                builder =>
                {
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(AuthenticationShemes.All)
                        .RequireClaim(System.Security.Claims.ClaimTypes.Sid)
                        .RequireAssertion(ctx =>
                            ctx.User.IsInRole(Roles.Owner) ||
                            ctx.User.AllowAction(Actions.SnapshotArchive));
                });
        }
    }
}
