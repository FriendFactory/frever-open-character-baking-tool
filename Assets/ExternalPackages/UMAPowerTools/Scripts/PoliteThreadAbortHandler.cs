using System;
using System.Threading;
using System.Collections.Generic;

namespace UMA.PowerTools
{
	public class PoliteThreadAbortHandler
	{
		volatile bool threadFlushed = false;
		volatile bool threadPoliteAbort = false;
		volatile bool threadPoliteAbort2 = false;
		AutoResetEvent triggerEvent;

		public class PoliteThreadAbortException : Exception {}

		public PoliteThreadAbortHandler(AutoResetEvent triggerEvent)
		{
			this.triggerEvent = triggerEvent;
		}

		public void Abort()
		{
			//signal the thread.
			threadPoliteAbort = true;
			triggerEvent.Set();
			threadPoliteAbort2 = true;

			// and wait
			while (!threadFlushed) Thread.Sleep(0);
			threadFlushed = false;
		}

		public void CheckForAbort()
		{
			if (threadPoliteAbort)
			{
				while (!threadPoliteAbort2) Thread.Sleep(0); // Sleep (0) helps if you're on a machine with only one cpu.
				threadPoliteAbort = false;
				threadPoliteAbort2 = false;
				triggerEvent.WaitOne(0); // first flush the trigger event, just in case

				throw new PoliteThreadAbortException();
			}
		}

		public void CompleteAbort()
		{
			// then tell main thread it's on
			threadFlushed = true;
			
			// and wait
			triggerEvent.WaitOne();
		}

		public void Resume()
		{
			triggerEvent.Set();
		}

		public int WaitAny(params WaitHandle[] events)
		{
			List<WaitHandle> eventList = new List<WaitHandle>(events);
			eventList.Add(triggerEvent);
			while (true)
			{
				var res = WaitHandle.WaitAny(eventList.ToArray());
				CheckForAbort();
				if (res < events.Length)
				{
					return res;
				}
			}
		}
	}


}
