using System;

namespace SlyPirates.Shared.Core.Ids.Attributes
{
	[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
	public class IdAttribute : Attribute
	{
		public readonly bool UseInt;

		public IdAttribute(bool useInt)
		{
			UseInt = useInt;
		}
	}
}
