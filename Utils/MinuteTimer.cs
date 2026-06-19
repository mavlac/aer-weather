using Microsoft.UI.Xaml;
using System;

namespace Aer.Utils
{
	public class MinuteTimer
	{
		private readonly DispatcherTimer _timer;
		private EventHandler<object?>? _tickHandler;

		public MinuteTimer()
		{
			_timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMinutes(1d)
			};
		}

		public void Start(Action callback)
		{
			// Ensure we don't add multiple handlers
			Stop();

			// Keep a strong reference to the handler so it can be removed later
			_tickHandler = (s, e) =>
			{
				// Avoid invoking callbacks while app is shutting down
				if (App.IsShuttingDown)
					return;
				callback();
			};

			_timer.Tick += _tickHandler;
			_timer.Start();
		}

		public void Stop()
		{
			_timer.Stop();
			if (_tickHandler != null)
			{
				_timer.Tick -= _tickHandler;
				_tickHandler = null;
			}
		}
	}
}
