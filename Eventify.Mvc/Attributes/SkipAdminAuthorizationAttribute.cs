using System;

namespace Eventify.Mvc.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipAdminAuthorizationAttribute : Attribute
    {
    }
} 