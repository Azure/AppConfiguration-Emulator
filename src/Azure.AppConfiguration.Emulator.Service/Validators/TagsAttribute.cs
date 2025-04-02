// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    class TagsAttribute : ModelBinderAttribute
    {
        public TagsAttribute() : base(typeof(TagsBinder))
        {
            BindingSource = BindingSource.Query;
        }
    }
}
