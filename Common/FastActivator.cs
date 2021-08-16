namespace Ecng.Common
{
	using System;
	using System.Reflection;
	using System.Reflection.Emit;

	public class FastEmitNotSupported
	{
	}

	/// <summary>
	/// Fast alternative to Activator.CreateInstance&lt;T> for reference types with default constructor
	/// </summary>
	/// <remarks>
	/// Измененная версия FastObjectFactory2&lt;T> отсюда:
	/// http://stackoverflow.com/questions/2024435/how-to-pass-ctor-args-in-activator-createinstance-or-use-il
	/// </remarks>
	public static class FastActivator<T>
		// чтобы можно было создавать классы с закрытым конструктором
		//where T :new()
	{
		public static bool NotSupported { get; set; }

		public static Func<T> CreateObject { get; }

		static FastActivator()
		{
			var objType = typeof(T);
			var cinfo = objType.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

			if ((NotSupported || Scope<FastEmitNotSupported>.Current != null) || objType.IsValueType || cinfo is null)
			{
				CreateObject = Activator.CreateInstance<T>;
			}
			else
			{
				DynamicMethod dynMethod;

				try
				{
					dynMethod = new DynamicMethod("DM$OBJ_FACTORY_" + objType.Name, objType, null, typeof(T), true);
				}
				catch (PlatformNotSupportedException)
				{
					NotSupported = true;
					CreateObject = Activator.CreateInstance<T>;
					return;
				}

				ILGenerator ilGen = dynMethod.GetILGenerator();
				ilGen.Emit(OpCodes.Newobj, cinfo);
				ilGen.Emit(OpCodes.Ret);
				CreateObject = (Func<T>)dynMethod.CreateDelegate(typeof(Func<T>));

				// mika
				// если будет Disposable объект, то проверочный запуск приведет к подвисшему в памяти объекту с неуправляемыми ресурсами
				//
				//try
				//{
				//    CreateObject();
				//}
				//catch
				//{
				//    CreateObject = () => new T();
				//}
			}
		}
	}
}
