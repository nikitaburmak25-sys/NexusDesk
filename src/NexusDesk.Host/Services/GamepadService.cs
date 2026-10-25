using System;
using System.Windows.Threading;

#if WINDOWS
using Windows.Gaming.Input;
#endif

namespace NexusDesk.Host.Services
{
    public enum GamepadAction
    {
        Left,
        Right,
        A,
        B
    }

    public class GamepadService
    {
        private readonly DispatcherTimer _timer;
        private DateTime _lastMoveTime = DateTime.MinValue;
        private bool _wasAPressed = false;
        private bool _wasBPressed = false;

        public event Action<GamepadAction>? OnAction;

        public GamepadService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
#if WINDOWS
            try
            {
                var gamepads = Gamepad.Gamepads;
                if (gamepads.Count == 0) return;

                var gamepad = gamepads[0];
                var reading = gamepad.GetCurrentReading();

                bool dpadLeft = (reading.Buttons & GamepadButtons.DPadLeft) == GamepadButtons.DPadLeft || reading.LeftThumbstickX < -0.5;
                bool dpadRight = (reading.Buttons & GamepadButtons.DPadRight) == GamepadButtons.DPadRight || reading.LeftThumbstickX > 0.5;
                bool aPressed = (reading.Buttons & GamepadButtons.A) == GamepadButtons.A;
                bool bPressed = (reading.Buttons & GamepadButtons.B) == GamepadButtons.B;

                var now = DateTime.Now;

                if (dpadLeft || dpadRight)
                {
                    if ((now - _lastMoveTime).TotalMilliseconds >= 200)
                    {
                        _lastMoveTime = now;
                        OnAction?.Invoke(dpadLeft ? GamepadAction.Left : GamepadAction.Right);
                    }
                }

                if (aPressed)
                {
                    if (!_wasAPressed)
                    {
                        _wasAPressed = true;
                        OnAction?.Invoke(GamepadAction.A);
                    }
                }
                else
                {
                    _wasAPressed = false;
                }

                if (bPressed)
                {
                    if (!_wasBPressed)
                    {
                        _wasBPressed = true;
                        OnAction?.Invoke(GamepadAction.B);
                    }
                }
                else
                {
                    _wasBPressed = false;
                }
            }
            catch
            {
                // Ignore if gamepad API fails
            }
#endif
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
