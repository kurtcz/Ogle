using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ogle
{
	public class LogsViewModel
	{
        public string? Layout { get; set; }
        public string? RoutePrefix { get; set; }
		public string Id { get; set; }
		public string HostName { get; set; }
		public DateOnly? Date { get; set; }
        public bool Highlight { get; set; }
        public List<SelectListItem> ServerSelectList { get; set; }
	}
}

