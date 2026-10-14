using System;
using System.Linq;

namespace SkyPirates.Shared.Core
{
	public static class TypeExt
	{
		public static string GetFormattedName(this Type type)
		{
			if (type.IsGenericType)
			{
				string genericArguments = type.GetGenericArguments()
									.Select(x => x.Name)
									.Aggregate((x1, x2) => $"{x1}, {x2}");
				return $"{type.Name[..type.Name.IndexOf("`")]}"
					 + $"<{genericArguments}>";
			}
			return type.Name;
		}
	}
}
