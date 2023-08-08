using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Linq;

namespace Ogle.Conventions
{
	public class AuthorizeAttributeConvention : IApplicationModelConvention
    {
        private readonly string _controllerName;
        private readonly string? _actionName;
        private readonly AuthorizeFilter[] _authorizeFilters;

        public AuthorizeAttributeConvention(string controllerName, params AuthorizeAttribute[] authorizeAttributes)
            : this(controllerName, null, authorizeAttributes)
        {
        }

        public AuthorizeAttributeConvention(string controllerName, string? actionName, params AuthorizeAttribute[] authorizeAttributes)
        {
            _controllerName = controllerName;
            _actionName = actionName;
            _authorizeFilters = authorizeAttributes.Select(i => new AuthorizeFilter(new[] { i })).ToArray();
        }

        public void Apply(ApplicationModel application)
        {
            var filterModels = application.Controllers
                                          .Where(i => string.Equals(i.ControllerName, _controllerName, StringComparison.OrdinalIgnoreCase))
                                          .ToList<IFilterModel>();

            if (filterModels.Count > 0 && !string.IsNullOrWhiteSpace(_actionName))
            {
                filterModels = filterModels.Cast<ControllerModel>()
                                           .SelectMany(i => i.Actions.Where(j => string.Equals(j.ActionName, _actionName, StringComparison.OrdinalIgnoreCase)))
                                           .ToList<IFilterModel>();
            }
            foreach (var filterModel in filterModels)
            {
                foreach (var authorizeFilter in _authorizeFilters)
                {
                    filterModel.Filters.Add(authorizeFilter);
                }
            }
        }
    }
}

