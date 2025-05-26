namespace BluOsNadRemote.App.ContentViews;

public partial class VerticalBarView : ContentView
{
    public static readonly BindableProperty PercentageProperty =
        BindableProperty.Create(
            nameof(Percentage),
            typeof(int),
            typeof(VerticalBarView),
            0,
            propertyChanged: OnPercentageChanged);

    public int Percentage
    {
        get => (int)GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public VerticalBarView()
    {
        InitializeComponent();
    }

    private static void OnPercentageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is VerticalBarView view)
        {
            view.UpdateFill();
        }
    }

    private void UpdateFill()
    {
        double percent = Math.Clamp(Percentage, 0, 100) / 100.0;
        double height = OuterBorder.Height * percent;
        FillBox.HeightRequest = height;
    }
}
