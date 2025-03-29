using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using BluOsNadRemote.App.Extensions;

namespace BluOsNadRemote.App.ContentViews;

[ContentProperty(nameof(MyContent))]
public partial class MaterialButton : ContentView
{
    private readonly Border _rippleEffect;
    private readonly Border _backgroundEffect;
    private readonly Grid _mainContainer;

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(MaterialButton), null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(MaterialButton), null);

    public static readonly BindableProperty RippleColorProperty =
        BindableProperty.Create(nameof(RippleColor), typeof(Color), typeof(MaterialButton), Colors.White);

    public static readonly BindableProperty ButtonColorProperty =
        BindableProperty.Create(
            nameof(ButtonColor),
            typeof(Color),
            typeof(MaterialButton),
            Colors.Transparent,
            propertyChanged: OnButtonColorChanged
        );

    private static void OnButtonColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialButton button && newValue is Color newColor)
        {
            button._backgroundEffect.BackgroundColor = newColor;
        }
    }
    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public Color RippleColor
    {
        get => (Color)GetValue(RippleColorProperty);
        set => SetValue(RippleColorProperty, value);
    }

    public Color ButtonColor
    {
        get => (Color)GetValue(ButtonColorProperty);
        set => SetValue(ButtonColorProperty, value);
    }

    public View MyContent
    {
        get => (View)_mainContainer.Children[0];
        set => _mainContainer.Children.Insert(0, value);
    }

    public MaterialButton()
    {

        // this.Content = _backgroundEffect.Content = _mainContainer.Children[0] = Content
        //                                                          .Children[1] = _rippleEffect
        _backgroundEffect = new Border
        {
            //StrokeShape = new RoundRectangle { CornerRadius = 6 },
            //Padding = new Thickness(12)
            Stroke = Brush.Transparent
        };
        var pointerEnterRecognizer = new PointerGestureRecognizer();
        pointerEnterRecognizer.PointerEntered += (s, e) => _backgroundEffect.AnimateBackgroundColor(this, ButtonColor);
        pointerEnterRecognizer.PointerExited += (s, e) => _backgroundEffect.AnimateBackgroundColor(this, ButtonColor, true);
        GestureRecognizers.Add(pointerEnterRecognizer);
        Content = _backgroundEffect;

        _mainContainer = new Grid { BackgroundColor = Colors.Transparent };
        _backgroundEffect.Content = _mainContainer;

        _rippleEffect = new Border
        {
            BackgroundColor = Colors.Transparent,
            StrokeThickness = 0,
            Opacity = 0,
            IsVisible = false,
            StrokeShape = new RoundRectangle { CornerRadius = 50 }
        };
        _mainContainer.Children.Add(_rippleEffect); // Add ripple effect (always at index 1)

        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) =>
        {
            await _rippleEffect.AnimateRipple(e.GetPosition(this), this);
            Command?.Execute(CommandParameter);
        };
        GestureRecognizers.Add(tapGestureRecognizer);
    }
}
