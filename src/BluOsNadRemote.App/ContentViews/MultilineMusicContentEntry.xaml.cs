namespace BluOsNadRemote.App.ContentViews;

public partial class MultilineMusicContentEntry : ContentView
{
    public static readonly BindableProperty ValueProperty =
         BindableProperty.Create(
            propertyName: nameof(Value),
             returnType: typeof(MusicContentEntryViewModel),
             declaringType: typeof(MultilineMusicContentEntry),
             defaultValue: null,
             defaultBindingMode: BindingMode.TwoWay);

    public MultilineMusicContentEntry()
    {
        InitializeComponent();
    }

    public MusicContentEntryViewModel Value
    {
        get { return (MusicContentEntryViewModel)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }
}
