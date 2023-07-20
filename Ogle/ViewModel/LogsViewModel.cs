using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ogle
{
	internal class LogsViewModel
	{
		public string Id { get; set; }
		public string HostName { get; set; }
		public string LogLines { get; set; }
		public DateOnly? Date { get; set; }
		public List<SelectListItem> ServerSelectList { get; set; }
	}
}

