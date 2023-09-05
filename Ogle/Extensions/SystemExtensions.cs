using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Ogle.Extensions
{
    public static class SystemExtensions
    {
        public static Dictionary<string, string> ToPropertyDictionary(this object instance)
        {
            var result = instance.GetType().GetProperties()
                                 .ToDictionary(k => k.Name, v =>
                                 {
                                     var result = v.GetValue(instance)?.ToString() ?? "null";

                                     if (v.PropertyType == typeof(string))
                                     {
                                         return $"\"{result}\"";
                                     }

                                     if (v.PropertyType == typeof(string[]))
                                     {
                                         var array = (string[]?)v.GetValue(instance);

                                         if (array == null)
                                         {
                                             return "[]";
                                         }

                                         result = string.Join(", ", array.Select(i => $"\"{i}\""));

                                         return $"[{result}]";
                                     }

                                     if (result.StartsWith("System.Func`"))
                                     {
                                         result = "{ ... }";
                                     }

                                     return result;
                                 });

            return result;
        }
    }
}

