namespace BluOsNadRemote.App.ContentViews;

public partial class SinglelineMusicContentEntry : ContentView
{
    public static readonly BindableProperty ValueProperty =
         BindableProperty.Create(
            propertyName: nameof(Value),
             returnType: typeof(MusicContentEntryViewModel),
             declaringType: typeof(SinglelineMusicContentEntry),
             defaultValue: null,
             defaultBindingMode: BindingMode.TwoWay);

    public SinglelineMusicContentEntry()
    {
        InitializeComponent();
    }

    public MusicContentEntryViewModel Value
    {
        get { return (MusicContentEntryViewModel)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }
}