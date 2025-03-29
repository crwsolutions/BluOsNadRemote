namespace BluOsNadRemote.App.Extensions;

internal static class BorderExtensions
{
    internal static async Task AnimateRipple(this Border border, Point? touchPoint, VisualElement element)
    {
        if (touchPoint is null)
        {
            return;
        }

        border.IsVisible = true;
        border.BackgroundColor = Colors.White;
        border.Opacity = 0.3;
        border.Scale = 5;

        var maxSize = Math.Max(element.Width, element.Height) * 2;
        border.WidthRequest = 8;
        border.HeightRequest = 8;

        // Convert touch position to _mainContainer's coordinate system
        var adjustedX = touchPoint.Value.X - border.WidthRequest / 2;
        var adjustedY = touchPoint.Value.Y - border.HeightRequest / 2;

        border.TranslationX = adjustedX - element.Width / 2;
        border.TranslationY = adjustedY - element.Height / 2;

        await Task.WhenAny(
            border.ScaleTo(maxSize / 10, 600, Easing.Linear),
            border.FadeTo(0, 400, Easing.Linear)
        );

        border.IsVisible = false;
        border.Opacity = 0;
    }

    internal static void AnimateBackgroundColor(this Border border, IAnimatable animatable, Color startColor, bool reverse = false)
    {
        // Calculate a lighter tint of the startColor
        var lightenFactor = 0.1f; // Adjust this factor to control how much lighter the endColor should be
        var endColor = new Color(
            Math.Min(startColor.Red + lightenFactor, 1.0f),
            Math.Min(startColor.Green + lightenFactor, 1.0f),
            Math.Min(startColor.Blue + lightenFactor, 1.0f),
            startColor.Alpha // Keep the alpha value the same
        );

        if (reverse)
        {
            (endColor, startColor) = (startColor, endColor);
        }

        var animation = new Animation(v =>
        {
            border.BackgroundColor = new Color(
                startColor.Red + (float)v * (endColor.Red - startColor.Red),
                startColor.Green + (float)v * (endColor.Green - startColor.Green),
                startColor.Blue + (float)v * (endColor.Blue - startColor.Blue),
                startColor.Alpha + (float)v * (endColor.Alpha - startColor.Alpha)
            );
        }, 0, 1);

        animation.Commit(animatable, "ColorAnimation", 16, 200, Easing.CubicIn);
    }
}
