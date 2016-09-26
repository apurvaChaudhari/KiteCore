using System.Timers;

namespace KiteCore.Helpers
{
    public class Watchdog
    {
        private readonly Timer _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Watchdog"/> class.
        /// This is used to emulate a timeout function to handle disruption in websocket connections
        /// </summary>
        /// <param name="seconds">Watchdog timer period</param>
        public Watchdog(int seconds)
        {
            this._timer = new Timer
            {
                Enabled = true,
                Interval = 1000*seconds
            };
        }

        /// <summary>
        /// event handler to take action on watchdog expiry
        /// </summary>
        public event ElapsedEventHandler OnTimerExpired;

        /// <summary>
        /// starts the watchdog timer
        /// </summary>
        public void Go()
        {
            this._timer.Elapsed += OnTimerExpired;
            this._timer.Start();
        }

        /// <summary>
        /// Stops the watchdog timer.
        /// </summary>
        public void Stop()
        {
            this._timer.Stop();
        }

        /// <summary>
        /// Resets the watchdog timer
        /// </summary>
        public void Reset()
        {
            this._timer.Stop();
            this._timer.Start();
        }
    }
}