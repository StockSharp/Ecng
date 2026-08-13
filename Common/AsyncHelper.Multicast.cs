namespace Ecng.Common;

public static partial class AsyncHelper
{
	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3>(
		this Func<T1, T2, T3, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3>(
		this Func<T1, T2, T3, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4>(
		this Func<T1, T2, T3, T4, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4>(
		this Func<T1, T2, T3, T4, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5>(
		this Func<T1, T2, T3, T4, T5, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5>(
		this Func<T1, T2, T3, T4, T5, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6>(
		this Func<T1, T2, T3, T4, T5, T6, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6>(
		this Func<T1, T2, T3, T4, T5, T6, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7>(
		this Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7>(
		this Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <typeparam name="T13">The type of the thirteenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="arg13">The thirteenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		T13 arg13,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.arg13,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <typeparam name="T13">The type of the thirteenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="arg13">The thirteenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		T13 arg13,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.arg13,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <typeparam name="T13">The type of the thirteenth handler argument.</typeparam>
	/// <typeparam name="T14">The type of the fourteenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="arg13">The thirteenth handler argument.</param>
	/// <param name="arg14">The fourteenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		T13 arg13,
		T14 arg14,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.arg13,
				state.arg14,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <typeparam name="T13">The type of the thirteenth handler argument.</typeparam>
	/// <typeparam name="T14">The type of the fourteenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="arg13">The thirteenth handler argument.</param>
	/// <param name="arg14">The fourteenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		T13 arg13,
		T14 arg14,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.arg13,
				state.arg14,
				state.cancellationToken))).AsTask();

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <typeparam name="T13">The type of the thirteenth handler argument.</typeparam>
	/// <typeparam name="T14">The type of the fourteenth handler argument.</typeparam>
	/// <typeparam name="T15">The type of the fifteenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="arg13">The thirteenth handler argument.</param>
	/// <param name="arg14">The fourteenth handler argument.</param>
	/// <param name="arg15">The fifteenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static ValueTask InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, CancellationToken, ValueTask> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		T13 arg13,
		T14 arg14,
		T15 arg15,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, cancellationToken),
			static (h, state) => h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.arg13,
				state.arg14,
				state.arg15,
				state.cancellationToken));

	/// <summary>
	/// Invokes and awaits every handler in the multicast asynchronous delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first handler argument.</typeparam>
	/// <typeparam name="T2">The type of the second handler argument.</typeparam>
	/// <typeparam name="T3">The type of the third handler argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth handler argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth handler argument.</typeparam>
	/// <typeparam name="T6">The type of the sixth handler argument.</typeparam>
	/// <typeparam name="T7">The type of the seventh handler argument.</typeparam>
	/// <typeparam name="T8">The type of the eighth handler argument.</typeparam>
	/// <typeparam name="T9">The type of the ninth handler argument.</typeparam>
	/// <typeparam name="T10">The type of the tenth handler argument.</typeparam>
	/// <typeparam name="T11">The type of the eleventh handler argument.</typeparam>
	/// <typeparam name="T12">The type of the twelfth handler argument.</typeparam>
	/// <typeparam name="T13">The type of the thirteenth handler argument.</typeparam>
	/// <typeparam name="T14">The type of the fourteenth handler argument.</typeparam>
	/// <typeparam name="T15">The type of the fifteenth handler argument.</typeparam>
	/// <param name="handler">The multicast asynchronous delegate.</param>
	/// <param name="arg1">The first handler argument.</param>
	/// <param name="arg2">The second handler argument.</param>
	/// <param name="arg3">The third handler argument.</param>
	/// <param name="arg4">The fourth handler argument.</param>
	/// <param name="arg5">The fifth handler argument.</param>
	/// <param name="arg6">The sixth handler argument.</param>
	/// <param name="arg7">The seventh handler argument.</param>
	/// <param name="arg8">The eighth handler argument.</param>
	/// <param name="arg9">The ninth handler argument.</param>
	/// <param name="arg10">The tenth handler argument.</param>
	/// <param name="arg11">The eleventh handler argument.</param>
	/// <param name="arg12">The twelfth handler argument.</param>
	/// <param name="arg13">The thirteenth handler argument.</param>
	/// <param name="arg14">The fourteenth handler argument.</param>
	/// <param name="arg15">The fifteenth handler argument.</param>
	/// <param name="cancellationToken">The cancellation token passed to every handler.</param>
	/// <returns>A task representing all handler invocations.</returns>
	/// <exception cref="AggregateException">Thrown after all handlers finish if multiple handlers are subscribed and one or more handlers fail.</exception>
	public static Task InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
		this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, CancellationToken, Task> handler,
		T1 arg1,
		T2 arg2,
		T3 arg3,
		T4 arg4,
		T5 arg5,
		T6 arg6,
		T7 arg7,
		T8 arg8,
		T9 arg9,
		T10 arg10,
		T11 arg11,
		T12 arg12,
		T13 arg13,
		T14 arg14,
		T15 arg15,
		CancellationToken cancellationToken = default)
		=> handler.InvokeAsync(
			(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, cancellationToken),
			static (h, state) => new(h(
				state.arg1,
				state.arg2,
				state.arg3,
				state.arg4,
				state.arg5,
				state.arg6,
				state.arg7,
				state.arg8,
				state.arg9,
				state.arg10,
				state.arg11,
				state.arg12,
				state.arg13,
				state.arg14,
				state.arg15,
				state.cancellationToken))).AsTask();
}
