using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace Eventify.Mvc.Attributes
{
    public class UserAuthorizationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var token = context.HttpContext.Session.GetString("token");
            var isAdmin = context.HttpContext.Session.GetString("isAdmin") == "true";
            
            if (string.IsNullOrEmpty(token))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            
            // Admin kullanıcılar normal kullanıcı sayfalarına erişebilir
            // Bu sayede admin hem admin hem de normal kullanıcı özelliklerini kullanabilir
            base.OnActionExecuting(context);
        }
    }
} 