using System;

namespace Ecng.Xaml.Charting.Common {
    public static class Maybe {
        public static TResult With2<TInput, TResult>(this TInput? o, Func<TInput, TResult> eval) where TInput:struct where TResult:class {
            return o!=null ? eval(o.Value) : null;
        }

        public static TResult With<TInput, TResult>(this TInput o, Func<TInput, TResult> eval) where TInput:class where TResult:class {
            return o!=null ? eval(o) : null;
        }

        public static TResult Return<TInput, TResult>(this TInput o, Func<TInput, TResult> eval, TResult failureValue) where TInput:class {
            return o!=null ? eval(o) : failureValue;
        }

        public static TResult Return2<TInput, TResult>(this TInput? o, Func<TInput, TResult> eval, TResult failureValue) where TInput:struct {
            return o!=null ? eval(o.Value) : failureValue;
        }

        public static TInput If<TInput>(this TInput o, Func<TInput, bool> eval) where TInput:class {
            return (o!=null && eval(o)) ? o : null;
        }

        public static TInput Unless<TInput>(this TInput o, Func<TInput, bool> eval) where TInput:class {
            return (o!=null && !eval(o)) ? o : null;
        }

        public static TInput Do<TInput>(this TInput o, Action<TInput> action) where TInput:class {
            if(o!=null) action(o);
            return o;
        }
    }
}
