using System.Collections.Generic;

namespace Ogle
{
    public class ConfigurationViewModel
    {
        public string? Layout { get; set; }
        public Dictionary<string, string> OgleRegistration { get; set; }
        public Dictionary<string, string> OgleOptions { get; set; }
        public Dictionary<string, string>? OgleRepositoryOptions { get; set; }
    }
}

