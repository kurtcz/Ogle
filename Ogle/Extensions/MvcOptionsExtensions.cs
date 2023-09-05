using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Ogle.Conventions;

namespace Ogle.Extensions
{
    public static class MvcOptionsExtensions
    {
        private static string OgleControllerName = nameof(OgleController).Replace("Controller", string.Empty);

        public static MvcOptions AddOgleAuthorizeAttributes(this MvcOptions options, params AuthorizeAttribute[] authorizeAttributes)
        {
            return options.AddOgleAuthorizeAttributes(null, authorizeAttributes);
        }

        public static MvcOptions AddOgleAuthorizeAttributes(this MvcOptions options, string actionName, params AuthorizeAttribute[] authorizeAttributes)
        {
            options.Conventions.Add(new AuthorizeAttributeConvention(OgleControllerName, actionName, authorizeAttributes));

            return options;
        }

        public static MvcOptions AddOgleAuthorizationPolicy(this MvcOptions options, params string[] policies)
        {
            return options.AddOgleAuthorizationPolicy(null, policies);
        }

        public static MvcOptions AddOgleAuthorizationPolicy(this MvcOptions options, string actionName, params string[] policies)
        {
            return options.AddOgleAuthorizeAttributes(actionName, policies.Select(i => new AuthorizeAttribute(i)).ToArray());
        }

        public static MvcOptions UseOgleRoutePrefix(this MvcOptions options, string prefix)
        {
            options.Conventions.Add(new RoutePrefixConvention(OgleControllerName, prefix));

            return options;
        }
    }
}

