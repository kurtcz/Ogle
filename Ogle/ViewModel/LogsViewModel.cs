using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ogle
{
	public class LogsViewModel
	{
		public string RoutePrefix { get; set; }
		public string Id { get; set; }
		public string HostName { get; set; }
		public string LogLines { get; set; }
		public DateOnly? Date { get; set; }
		public List<SelectListItem> ServerSelectList { get; set; }
	}
}

