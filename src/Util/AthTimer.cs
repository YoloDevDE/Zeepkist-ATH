using System;
using System.Timers;

namespace AuthorTimeHunting.Util;

public class AthTimer
{
    private readonly Timer _timer;

    public AthTimer()
    {
        _timer = new Timer(1000);
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
    }

    public event Action Tick;

    private void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        Tick?.Invoke();
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public void Stop()
    {
        _timer.Stop();
    }
}