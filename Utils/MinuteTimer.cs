using Microsoft.UI.Xaml;
using System;

namespace Aer.Utils
{
	public class MinuteTimer
	{
		private readonly DispatcherTimer _timer;

		public MinuteTimer(TimeSpan? interval = null)
		{
			_timer = new DispatcherTimer
			{
				Interval = interval ?? TimeSpan.FromMinutes(1d)
			};
		}

		public void Start(Action callback)
		{
			_timer.Tick += (s, e) => callback();
			_timer.Start();
		}

		public void Stop()
		{
			_timer.Stop();
			_timer.Tick -= null; // optional: no-op, just here for clarity
		}
	}
}
