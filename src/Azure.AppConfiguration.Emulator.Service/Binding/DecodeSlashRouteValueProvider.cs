// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using System.Text;

namespace Azure.AppConfiguration.Emulator.Service
{
    /// <summary>
    /// This provider decodes %2f from request path.
    /// MVC databinding doesn't decode %2f from route parameters. It does decode everything else though.
    /// See: https://github.com/aspnet/Mvc/issues/6388
    /// </summary>
    class DecodeSlashRouteValueProvider : RouteValueProvider
    {
        public DecodeSlashRouteValueProvider(BindingSource bindingSource, RouteValueDictionary values)
            : base(bindingSource, values)
        {
        }

        public override ValueProviderResult GetValue(string key)
        {
            ValueProviderResult result = base.GetValue(key);

            if (result != default &&
                result != ValueProviderResult.None &&
                TryDecodeSlash(result.Values, out string decoded))
            {
                result = new ValueProviderResult(decoded, result.Culture);
            }

            return result;
        }

        private static bool TryDecodeSlash(string value, out string result)
        {
            result = null;

            if (string.IsNullOrEmpty(value) || value.Length < 3)
            {
                return false;
            }

            StringBuilder sb = null;
            int pos = 0;

            for (int i = 0; i < value.Length - 2; ++i)
            {
                if (value[i] == '%' &&
                    value[i + 1] == '2' &&
                    char.ToLower(value[i + 2]) == 'f')
                {
                    sb = sb ?? new StringBuilder(value.Length);

                    sb.Append(value.Substring(pos, i - pos))
                      .Append('/');

                    i += 2;
                    pos = i + 1;
                }
            }

            if (sb != null)
            {
                if (pos < value.Length)
                {
                    sb.Append(value.Substring(pos));
                }

                result = sb.ToString();
            }

            return result != null;
        }
    }
}
