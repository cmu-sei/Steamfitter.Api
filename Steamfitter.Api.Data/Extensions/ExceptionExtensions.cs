// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;

namespace Steamfitter.Api.Data.Extensions
{
    public static class ExceptionExtensions
    {
        public static bool IsTransientDatabaseException(this Exception exception)
        {
            Exception ex = exception;

            while (ex != null)
            {
                if ((exception is InvalidOperationException ||
                    exception is DbUpdateException) &&
                    exception.Message.Contains("transient"))
                {
                    return true;
                }

                ex = exception.InnerException;
            }

            return false;
        }
    }
}
