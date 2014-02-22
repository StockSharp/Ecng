namespace Ecng.Common
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Drawing;
#if SILVERLIGHT
	using System.Windows.Media;
#else
	using System.Security.Cryptography;
	using System.Xml;
#endif

	#endregion

	public static class CompareHelper
	{
#if !SILVERLIGHT
		public static bool Compare(this Image first, Image second)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			if (first.Size == second.Size)
			{
				using (HashAlgorithm hashAlg = new SHA256Managed())
				{
					var converter = new ImageConverter();

					var firstHash = hashAlg.ComputeHash((byte[])converter.ConvertTo(first, typeof(byte[])));
					var secondHash = hashAlg.ComputeHash((byte[])converter.ConvertTo(second, typeof(byte[])));

					if (firstHash.Length == secondHash.Length)
					{
						//Compare the hash values
						for (var i = 0; i < firstHash.Length; i++)
						{
							if (firstHash[i] != secondHash[i])
								return false;
						}
					}
					else
						return false;
				}
			}
			else
				return false;

			return true;
		}

		public static bool Compare(this XmlNode first, XmlNode second)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			return first.OuterXml == second.OuterXml;
		}
#endif

		public static int Compare(this IPAddress first, IPAddress second)
		{
			return first.To<long>().CompareTo(second.To<long>());
		}

		public static bool Compare(this Type first, Type second, bool useInheritance)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			if (useInheritance)
				return first.IsAssignableFrom(second);
			else
				return first == second;
		}

		public static int Compare(this Type first, Type second)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			if (first == second)
				return 0;
			else if (first.IsAssignableFrom(second))
				return 1;
			else
				return -1;
		}

		public static bool Compare(this Color first, Color second)
		{
			return first.ToArgb() == second.ToArgb();
		}

		public static int Compare(this object value1, object value2)
		{
			if (value1 == null && value2 == null)
				return 0;

			if (value1 == null)
				return -1;

			if (value2 == null)
				return 1;

			if (value1.GetType() != value2.GetType())
				throw new ArgumentException("The values must be a same types.", "value2");

			var compare1 = value1 as IComparable;

			if (compare1 != null)
				return compare1.CompareTo(value2);

			throw new ArgumentException("The values must be IComparable.");
		}

		public static bool IsDefault<T>(this T value)
		{
			return EqualityComparer<T>.Default.Equals(value, default(T));
		}

		public static bool IsRuntimeDefault<T>(this T value)
		{
			return EqualityComparer<T>.Default.Equals(value, (T)value.GetType().GetDefaultValue());
		}
	}
}