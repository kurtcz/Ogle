using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Ogle.Conventions
{
    public class RoutePrefixConvention : IApplicationModelConvention
    {
        private readonly string _controllerName;
        private readonly string _routePrefix;

        public RoutePrefixConvention(string controllerName, string routePrefix)
        {
            _controllerName = controllerName;
            _routePrefix = routePrefix;
        }

        public void Apply(ApplicationModel application)
        {
            var controller = application.Controllers
                                        .SingleOrDefault(i => string.Equals(i.ControllerName, _controllerName, StringComparison.OrdinalIgnoreCase));

            if (controller != null)
            {
                foreach (var selector in controller.Selectors)
                {
                    if (selector.AttributeRouteModel?.Attribute?.Template != null)
                    {
                        var route = selector.AttributeRouteModel.Attribute.Template.Replace("/ogle", _routePrefix);

                        selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(route));
                    }
                }
                foreach (var action in controller.Actions)
                {
                    foreach (var selector in action.Selectors)
                    {
                        if (selector.AttributeRouteModel?.Attribute?.Template != null)
                        {
                            var route = selector.AttributeRouteModel.Attribute.Template.Replace("/ogle", _routePrefix);

                            selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(route));
                        }
                    }
                }
            }
        }
    }
}

