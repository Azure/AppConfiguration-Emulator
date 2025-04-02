// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Utils;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    class TagsBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            string tagsParameterName = bindingContext.FieldName;

            List<KeyValuePair<string, string>> tagFilters = null;

            if (!string.IsNullOrWhiteSpace(tagsParameterName) &&
                bindingContext.HttpContext.Request.Query.TryGetValue(tagsParameterName, out StringValues inputValues))
            {
                tagFilters = new List<KeyValuePair<string, string>>();

                foreach (string tagFilter in inputValues)
                {
                    if (!string.IsNullOrWhiteSpace(tagFilter))
                    {
                        try
                        {
                            tagFilters.Add(SearchQueryHelper.ParseTagFilter(tagFilter.AsSpan()));
                        }
                        catch (SearchQueryException)
                        {
                            bindingContext.ModelState.AddModelError(
                                tagsParameterName,
                                "Invalid parameter.");

                            bindingContext.Result = ModelBindingResult.Failed();

                            return Task.CompletedTask;
                        }
                    }
                }
            }

            bindingContext.Result = ModelBindingResult.Success(tagFilters);

            return Task.CompletedTask;
        }
    }
}
