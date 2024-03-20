namespace BluOsNadRemote.App.ContentViews;

public partial class FormItem : ContentView
{
    public static readonly BindableProperty LabelProperty =
            BindableProperty.Create(nameof(Label),
            typeof(string), typeof(FormItem));

    public FormItem()
    {
        InitializeComponent();
    }

    public string Label
    {

        get => GetValue(LabelProperty) as string;
        set => SetValue(LabelProperty, value);

    }
}
