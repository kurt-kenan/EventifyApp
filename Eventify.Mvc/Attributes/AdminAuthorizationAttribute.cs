using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace Eventify.Mvc.Attributes
{
    public class AdminAuthorizationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // SkipAdminAuthorization attribute'u varsa kontrolü atla
            var actionMethod = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
            if (actionMethod != null)
            {
                var skipAttribute = actionMethod.MethodInfo.GetCustomAttribute<SkipAdminAuthorizationAttribute>();
                if (skipAttribute != null)
                {
                    base.OnActionExecuting(context);
                    return;
                }
            }

            var isAdmin = context.HttpContext.Session.GetString("isAdmin") == "true";
            
            if (!isAdmin)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            
            base.OnActionExecuting(context);
        }
    }
} 