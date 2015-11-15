using System;

namespace Ecng.Xaml.Charting.Common {
    static class DelegateHelper {
        internal static void SafeInvoke(this Action handler) {
            var h = handler;

            if(h != null)
                h();
        }

        internal static void SafeInvoke<T>(this Action<T> handler, T arg) {
            var h = handler;

            if(h != null)
                h(arg);
        }

        internal static void SafeInvoke<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2 arg2) {
            var h = handler;

            if(h != null)
                h(arg1, arg2);
        }

        internal static void SafeInvoke<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3);
        }

        internal static void SafeInvoke<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6>(this Action<T1, T2, T3, T4, T5, T6> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7>(this Action<T1, T2, T3, T4, T5, T6, T7> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(this Action<T1, T2, T3, T4, T5, T6, T7, T8> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        internal static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) {
            var h = handler;

            if(h != null)
                h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        internal static void SafeInvoke(this EventHandler<EventArgs> handler, object sender) {
            handler.SafeInvoke(sender, EventArgs.Empty);
        }

        internal static void SafeInvoke<T>(this EventHandler<T> handler, object sender, T args) where T : EventArgs {
            handler.SafeInvoke(sender, args, args2 => { });
        }

        internal static void SafeInvoke<T>(this EventHandler<T> handler, object sender, T args, Action<T> action)
            where T : EventArgs {
            if(sender == null)
                throw new ArgumentNullException("sender");

            if(args == null)
                throw new ArgumentNullException("args");

            if(action == null)
                throw new ArgumentNullException("action");

            var handlerLocal = handler;
            if(handlerLocal != null) {
                handlerLocal(sender, args);
                action(args);
            }
        }
    }
}
