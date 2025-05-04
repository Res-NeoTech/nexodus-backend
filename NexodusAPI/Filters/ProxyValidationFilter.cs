using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using NexodusAPI.Utils;
using NexodusAPI.Attributes;

namespace NexodusAPI.Filters
{
    public class ProxyValidationFilter : IActionFilter
    {
        private readonly ILogger<ProxyValidationFilter> _logger;

        public ProxyValidationFilter(ILogger<ProxyValidationFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<BypassProxyValidationAttribute>().Any() ||
                context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor &&
                controllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(BypassProxyValidationAttribute), true).Any())
            {
                return; // Skip the filter logic.
            }

            if (!context.HttpContext.Request.Headers.TryGetValue("x-nexodus-proxy", out var headerValues))
            {
                context.Result = new UnauthorizedObjectResult("We couldn't make sure your request is authorized.");
                _logger.LogWarning($"Unauthorized access attempt at {DateTime.Now}");
            }
            else
            {
                var headerValue = headerValues.FirstOrDefault();
                if (string.IsNullOrEmpty(headerValue) || !IsValidProxyToken(headerValue))
                {
                    context.Result = new UnauthorizedObjectResult("We couldn't make sure your request is authorized.");
                    _logger.LogWarning($"Unauthorized access attempt at {DateTime.Now}");
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed here for now.
        }

        private bool IsValidProxyToken(string token)
        {
            const string VALID_TOKEN = "TOKEN"; // Secret value.
            try
            {
                return Cryptography.DecodeFromBase64(token) == VALID_TOKEN;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Proxy Token Exception at {DateTime.Now} Message: {ex.Message}");
                return false;
            }
        }
    }
}