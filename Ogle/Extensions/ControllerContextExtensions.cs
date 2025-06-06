using Microsoft.AspNetCore.Mvc;

namespace Ogle.Extensions
{
    public static class ControllerContextExtensions
    {
        public static string GetRoutePrefix(this ControllerContext context)
        {
            var route = context.ActionDescriptor.AttributeRouteInfo.Template;
            var slashIndex = route.LastIndexOf('/');

            if (slashIndex >= 0)
            {
                return route.Substring(0, slashIndex);
            }

            return route;
        }
    }
}

