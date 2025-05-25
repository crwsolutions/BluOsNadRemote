namespace BluOsNadRemote.App.ContentViews;

public partial class SpectrumAnalyzerView : ContentView
{
    private readonly Random _random = new();
    private readonly System.Timers.Timer _timer;
    private readonly double[] _targetHeights = [MinimumHeight, MinimumHeight, MinimumHeight];
    private readonly double[] _currentHeights = [MinimumHeight, MinimumHeight, MinimumHeight];
    private const double AnimationStep = 0.08; // Controls smoothness
    private const double MinimumHeight = 0.05;

    public static readonly BindableProperty IsPlayingProperty =
        BindableProperty.Create(
            nameof(IsPlaying),
            typeof(bool),
            typeof(SpectrumAnalyzerView),
            false,
            propertyChanged: OnIsPlayingChanged);

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public SpectrumAnalyzerView()
    {
        InitializeComponent();
        _timer = new System.Timers.Timer(50); // Slower animation
        _timer.Elapsed += (s, e) => MainThread.BeginInvokeOnMainThread(AnimateBars);
    }

    private static void OnIsPlayingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SpectrumAnalyzerView view && (bool)newValue)
        {
            view._timer.Start();
            Debug.WriteLine("Started timer");
        }
    }

    private void AnimateBars()
    {
        for (int i = 0; i < 3; i++)
        {
            // Only pick a new target if we're close to the previous one
            if (Math.Abs(_currentHeights[i] - _targetHeights[i]) < 0.2)
            {
                _targetHeights[i] = IsPlaying
                    ? _random.NextDouble() * 0.85 + 0.15 // 15% to 85%
                    : MinimumHeight;
            }
        }

        bool needsUpdate = false;
        for (int i = 0; i < _currentHeights.Length; i++)
        {
            double diff = _targetHeights[i] - _currentHeights[i];
            if (Math.Abs(diff) > MinimumHeight)
            {
                _currentHeights[i] += diff * AnimationStep;
                needsUpdate = true;
            }
            else
            {
                _currentHeights[i] = _targetHeights[i];
            }
        }
        if (needsUpdate)
        {
            UpdateBarLayout();
        }

        if (IsPlaying == false && _currentHeights[0] == MinimumHeight && _currentHeights[1] == MinimumHeight && _currentHeights[2] == MinimumHeight)
        {
            UpdateBarLayout();
            _timer.Stop();
            Debug.WriteLine("Stopped timer");
        }
    }

    private void UpdateBarLayout()
    {
        try
        {
            double totalWidth = RootGrid.Width;
            double totalHeight = RootGrid.Height;
            if (totalWidth <= 0 || totalHeight <= 0) return;

            double barWidth = totalWidth * 0.25;

            Bar1.WidthRequest = barWidth;
            Bar2.WidthRequest = barWidth;
            Bar3.WidthRequest = barWidth;

            Bar1.HeightRequest = Math.Max(0, totalHeight * _currentHeights[0]);
            Bar2.HeightRequest = Math.Max(0, totalHeight * _currentHeights[1]);
            Bar3.HeightRequest = Math.Max(0, totalHeight * _currentHeights[2]);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error updating bar layout: {e.Message}");
        }
    }
}
