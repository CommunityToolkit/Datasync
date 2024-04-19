// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Datasync.Server.Test;

/// <summary>
/// An object validator so that TryValidateModel will work on a controller.
/// </summary>
[ExcludeFromCodeCoverage]
internal class ObjectValidator : IObjectModelValidator
{
    public void Validate(ActionContext actionContext, ValidationStateDictionary validationState, string prefix, object model)
    {
        ValidationContext context = new(model, serviceProvider: null, items: null);
        List<ValidationResult> results = [];
        bool isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        if (!isValid)
        {
            results.ForEach(r => actionContext.ModelState.AddModelError(r.MemberNames.FirstOrDefault() ?? "", r.ErrorMessage));
        }
    }
}
