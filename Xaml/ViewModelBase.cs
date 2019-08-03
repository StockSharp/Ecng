namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq.Expressions;

	using Ecng.Common;

	/// <summary>
	/// Базовый класс для реализации View-Model в модели MVVM
	/// </summary>
	public abstract class ViewModelBase : Disposable, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		protected void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression)
		{
			OnPropertyChanged(PropertyName(selectorExpression));
		}

		protected bool SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
		{
			return SetField(ref field, value, PropertyName(selectorExpression));
		}

		// установка требуемого поля в определенное значение и вызов события PropertyChanged при необходимости
		protected virtual bool SetField<T>(ref T field, T value, string name)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;

			field = value;
			OnPropertyChanged(name);

			return true;
		}

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
