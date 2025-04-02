// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    static class ExceptionExtensions
    {
        private static readonly byte[] ErrorCodes = Encoding.UTF8.GetBytes("error_codes");

        private static readonly object Key = new object();

        public static AuthenticationError GetAuthenticationError(this MsalServiceException e)
        {
            //
            // Check the cache first
            if (e.Data.Contains(Key))
            {
                return (AuthenticationError)e.Data[Key];
            }

            //
            // Check for known errors
            AuthenticationError error = null;

            if (e.StatusCode == (int)HttpStatusCode.BadRequest)
            {
                string errorCode = GetErrorCodeFromJson(e.ResponseBody);

                if (errorCode == Errors.ConfidentialClient.Code)
                {
                    error = Errors.ConfidentialClient;
                }
                else if (errorCode == Errors.UnsupportedTenantHost.Code)
                {
                    error = Errors.UnsupportedTenantHost;
                }
                else if (errorCode == Errors.TenantFederationError.Code)
                {
                    error = Errors.TenantFederationError;
                }
                else if (errorCode == Errors.NationalCloudTenantRedirection.Code)
                {
                    error = Errors.NationalCloudTenantRedirection;
                }
            }

            //
            // Cache the error
            e.Data[Key] = error;

            return error;
        }

        public static AuthenticationError GetKnownError(this AggregateException ex)
        {
            if (ex?.InnerExceptions == null)
            {
                return null;
            }

            foreach (Exception e in ex.InnerExceptions)
            {
                if (e is MsalServiceException msalServiceException)
                {
                    AuthenticationError error = msalServiceException.GetAuthenticationError();

                    if (error != null)
                    {
                        return error;
                    }
                }
            }

            return null;
        }

        public static T GetException<T>(this Exception e) where T : Exception
        {
            T result = e as T ?? e.InnerException as T;

            //
            // Lookup up in aggregated exception
            if (result == null && e is AggregateException aggregated)
            {
                Exception inner = aggregated.InnerExceptions.FirstOrDefault(ie => ie is T || ie.InnerException is T);

                if (inner != null)
                {
                    return inner as T ?? inner.InnerException as T;
                }
            }

            return result;
        }

        /// <summary>
        /// Parse OAuth2 error
        /// see https://docs.microsoft.com/en-us/azure/active-directory/develop/reference-aadsts-error-codes#handling-error-codes-in-your-application
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static string GetErrorCodeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(ErrorCodes) &&
                        reader.Read() &&
                        reader.TokenType == JsonTokenType.StartArray &&
                        reader.Read())
                    {
                        return Encoding.UTF8.GetString(reader.ValueSpan);
                    }
                }
            }

            return null;
        }
    }
}
