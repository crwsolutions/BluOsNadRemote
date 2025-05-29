namespace BluOsNadRemote.App.ContentViews;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class RemoteStepper : ContentView
{
    public static readonly BindableProperty ValueProperty =
          BindableProperty.Create(
             propertyName: nameof(Value),
              returnType: typeof(int),
              declaringType: typeof(RemoteStepper),
              defaultValue: 1,
              defaultBindingMode: BindingMode.TwoWay);

    public int? Maximum { get; set; }
    public int? Minimum { get; set; }

    public int Increment { get; set; } = 1;

    public int Value
    {
        get { return (int)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public Color ButtonColor { get; set; } = Colors.Black;

    public Color LabelColor { get; set; } = Colors.Black;

    public RemoteStepper()
    {
        InitializeComponent();
        lblValue.SetBinding(Label.TextProperty, new Binding(nameof(Value), BindingMode.TwoWay, source: this));
    }

    void btnPlus_Clicked(object sender, EventArgs e)
    {
        if (Maximum == null)
        {
            Value += Increment;
        }
        else if (Value < Maximum)
        {
            Value += Increment;
        }
    }

    void btnMinus_Clicked(object sender, EventArgs e)
    {
        if ((Value - Increment) > Minimum)
        {
            Value -= Increment;
        }
        else
        {
            Value = 0;
        }
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == ValueProperty.PropertyName)
        {
            lblValue.Text = Value.ToString();
        }
    }
}
