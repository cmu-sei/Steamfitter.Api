// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Steamfitter.Api.Infrastructure.Authorization;

namespace Steamfitter.Api.Infrastructure.Filters
{
    /// <summary>
    /// Hides from every response the guest credentials the caller may not read, whichever endpoint wrote it, so
    /// that a new endpoint returning Tasks, Results, Scenarios or Scenario Templates cannot forget to.
    /// </summary>
    public class SecretRedactionFilter(ISecretRedactor secretRedactor) : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult { Value: not null } result)
            {
                var redacted = await secretRedactor.RedactAsync(result.Value, context.HttpContext.RequestAborted);
                if (!ReferenceEquals(redacted, result.Value))
                {
                    // a sequence comes back as a list, which the type the action declared may not be
                    result.Value = redacted;
                    result.DeclaredType = null;
                }
            }

            await next();
        }
    }
}
