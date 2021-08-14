
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecng.Net.SocketIO.Engine.Thread
{
    public class EasyTimer
    {
        private readonly CancellationTokenSource _ts;

        private EasyTimer(CancellationTokenSource ts)
        {
	        _ts = ts ?? throw new ArgumentNullException(nameof(ts));
        }

        public static EasyTimer SetTimeout(Action method, int delayInMilliseconds, Action<Exception> errorHandler)
        {
	        if (errorHandler is null)
		        throw new ArgumentNullException(nameof(errorHandler));

	        var ts = new CancellationTokenSource();

            Task.Delay(delayInMilliseconds, ts.Token).GetAwaiter().OnCompleted(() =>
            {
                try
                {
	                if (!ts.IsCancellationRequested)
	                {
		                method();
	                }
                }
                catch (Exception e)
                {
	                errorHandler(e);
                }
            });
            
            // Returns a stop handle which can be used for stopping
            // the timer, if required
            return new EasyTimer(ts);
        }

        public void Stop()
        {
            //var log = LogManager.GetLogger(Global.CallerName());
            //log.Info("EasyTimer stop");
            _ts?.Cancel();                
        }


        //public static void TaskRun(Action action)
        //{
        //    Task.Run(action).Wait();
        //}

        //public static void TaskRunNoWait(Action action)
        //{
        //    Task.Run(action);
        //}
    }


}
