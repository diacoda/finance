using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Finance.Tracking.WebApi;

public class HttpResponseExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ArgumentException argEx)
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = argEx.Message
            });
            context.ExceptionHandled = true; // Mark as handled
        }
    }
}
