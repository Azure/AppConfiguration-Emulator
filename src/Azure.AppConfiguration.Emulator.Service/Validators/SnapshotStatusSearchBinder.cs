using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    class SnapshotStatusSearchBinder : IModelBinder
    {
        private const string StatusParameterName = "status";
        private static readonly Type ParameterType = typeof(SnapshotStatusSearch);

        private static class Statuses
        {
            public const string Archived = "archived";
            public const string Provisioning = "provisioning";
            public const string Ready = "ready";
            public const string Failed = "failed";
            public const string All = "*";
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (bindingContext.ModelType != ParameterType)
            {
                throw new InvalidOperationException(
                    $"{nameof(SnapshotStatusSearchBinder)} requires a parameter of type {nameof(SnapshotStatusSearch)}.");
            }

            SnapshotStatusSearch search = SnapshotStatusSearch.None;

            if (bindingContext.HttpContext.Request.Query.TryGetValue(StatusParameterName, out StringValues inputValue))
            {
                string sValue = inputValue.First();

                if (sValue == Statuses.All)
                {
                    search = SnapshotStatusSearch.All;
                }
                else
                {
                    int i = 0;

                    int start = 0;

                    while (i <= sValue.Length)
                    {
                        if (i == sValue.Length ||
                            sValue[i] == ',')
                        {
                            SnapshotStatusSearch? val = GetFlag(sValue.AsSpan(start, i - start));

                            if (!val.HasValue)
                            {
                                bindingContext.ModelState.AddModelError(
                                    StatusParameterName,
                                    "Invalid status.");

                                bindingContext.Result = ModelBindingResult.Failed();

                                return Task.CompletedTask;
                            }

                            search |= val.Value;

                            start = i + 1;
                        }

                        i++;
                    }
                }
            }
            else
            {
                search = SnapshotStatusSearch.All;
            }

            bindingContext.Result = ModelBindingResult.Success(search);

            return Task.CompletedTask;
        }

        private static SnapshotStatusSearch? GetFlag(ReadOnlySpan<char> status)
        {
            if (status.SequenceEqual(Statuses.Ready))
            {
                return SnapshotStatusSearch.Ready;
            }
            else if (status.SequenceEqual(Statuses.Provisioning))
            {
                return SnapshotStatusSearch.Provisioning;
            }
            else if (status.SequenceEqual(Statuses.Failed))
            {
                return SnapshotStatusSearch.Failed;
            }
            else if (status.SequenceEqual(Statuses.Archived))
            {
                return SnapshotStatusSearch.Archived;
            }
            else
            {
                return null;
            }
        }
    }
}
