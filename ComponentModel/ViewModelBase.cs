namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using System.ComponentModel;
	using System.Linq.Expressions;

	using Ecng.Common;

	/// <summary>
	/// Базовый класс для реализации View-Model в модели MVVM
	/// </summary>
	public abstract class ViewModelBase : Disposable, INotifyPropertyChanged
	{
		/// <inheritdoc />
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// </summary>
		protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>
		/// </summary>
		protected void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression)
		{
			OnPropertyChanged(PropertyName(selectorExpression));
		}

		/// <summary>
		/// </summary>
		protected bool SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
		{
			return SetField(ref field, value, PropertyName(selectorExpression));
		}

		/// <summary>
		/// установка требуемого поля в определенное значение и вызов события PropertyChanged при необходимости
		/// </summary>
		protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;

			field = value;
			OnPropertyChanged(name);

			return true;
		}

		/// <summary>
		/// </summary>
		public static string PropertyName<T>(Expression<Func<T>> property)
		{
			var lambda = (LambdaExpression)property;

			MemberExpression memberExpression;

			if (lambda.Body is UnaryExpression exp)
			{
				var unaryExpression = exp;
				memberExpression = (MemberExpression)unaryExpression.Operand;
			}
			else
			{
				memberExpression = (MemberExpression)lambda.Body;
			}

			return memberExpression.Member.Name;
		}
	}
}
