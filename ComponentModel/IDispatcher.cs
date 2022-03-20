﻿namespace Ecng.ComponentModel
{
	using System;

	/// <summary>
	/// Threads dispatcher.
	/// </summary>
	public interface IDispatcher
	{
		/// <summary>
		/// Call action in dispatcher thread.
		/// </summary>
		/// <param name="action">Action.</param>
		void Invoke(Action action);

		/// <summary>
		/// Call action in dispatcher thread.
		/// </summary>
		/// <param name="action">Action.</param>
		void InvokeAsync(Action action);

		bool CheckAccess();
	}

	public class DummyDispatcher : IDispatcher
	{
		bool IDispatcher.CheckAccess() => true;
		void IDispatcher.Invoke(Action action) => action();
		void IDispatcher.InvokeAsync(Action action) => action.BeginInvoke(action.EndInvoke, null);
	}
}