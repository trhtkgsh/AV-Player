using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Player.App.Features.PlayerHud.Controls;

/// <summary>
/// A white vector symbol without a background. Drawn with a Path, so no icon font or image is needed.
/// Tap runs <see cref="Command"/> and gives a short press feedback.
/// </summary>
public sealed class HudIconButton : ContentView
{
    public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
        nameof(IconData),
        typeof(Geometry),
        typeof(HudIconButton),
        null,
        propertyChanged: (bindable, _, newValue) =>
            ((HudIconButton)bindable)._icon.Data = (Geometry?)newValue);

    public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
        nameof(IconSize),
        typeof(double),
        typeof(HudIconButton),
        28d,
        propertyChanged: (bindable, _, newValue) =>
        {
            var button = (HudIconButton)bindable;
            button._icon.WidthRequest = (double)newValue;
            button._icon.HeightRequest = (double)newValue;
        });

    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor),
        typeof(Color),
        typeof(HudIconButton),
        Colors.White,
        propertyChanged: (bindable, _, newValue) =>
            ((HudIconButton)bindable)._icon.Fill = new SolidColorBrush(newValue as Color ?? Colors.White));

    public static readonly BindableProperty MirrorInRightToLeftProperty = BindableProperty.Create(
        nameof(MirrorInRightToLeft),
        typeof(bool),
        typeof(HudIconButton),
        false,
        propertyChanged: (bindable, _, newValue) =>
            ((HudIconButton)bindable)._icon.FlowDirection = FlowDirectionFor((bool)newValue));

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command),
        typeof(ICommand),
        typeof(HudIconButton));

    public static readonly BindableProperty TouchHeightProperty = BindableProperty.Create(
        nameof(TouchHeight),
        typeof(double),
        typeof(HudIconButton),
        48d,
        propertyChanged: (bindable, _, newValue) =>
            ((HudIconButton)bindable)._touchArea.MinimumHeightRequest = (double)newValue);

    private readonly Microsoft.Maui.Controls.Shapes.Path _icon;
    private readonly Grid _touchArea;

    public HudIconButton()
    {
        _icon = new Microsoft.Maui.Controls.Shapes.Path
        {
            Aspect = Stretch.Uniform,
            WidthRequest = IconSize,
            HeightRequest = IconSize,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Fill = new SolidColorBrush(IconColor),
            FlowDirection = FlowDirectionFor(MirrorInRightToLeft),
        };

        _touchArea = new Grid
        {
            BackgroundColor = Colors.Transparent,
            MinimumWidthRequest = 40,
            MinimumHeightRequest = TouchHeight,
        };
        _touchArea.Children.Add(_icon);

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        _touchArea.GestureRecognizers.Add(tap);

        Content = _touchArea;
    }

    /// <summary>The smallest height of the touch area, in units. Default 48; the bottom transport buttons use 44.</summary>
    public double TouchHeight
    {
        get => (double)GetValue(TouchHeightProperty);
        set => SetValue(TouchHeightProperty, value);
    }

    [TypeConverter(typeof(PathGeometryConverter))]
    public Geometry? IconData
    {
        get => (Geometry?)GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// False (default): the symbol is always drawn left to right, so transport controls are never mirrored.
    /// True: the symbol follows the page direction (a back arrow points right on a right-to-left screen).
    /// </summary>
    public bool MirrorInRightToLeft
    {
        get => (bool)GetValue(MirrorInRightToLeftProperty);
        set => SetValue(MirrorInRightToLeftProperty, value);
    }

    private static FlowDirection FlowDirectionFor(bool mirror) =>
        mirror ? FlowDirection.MatchParent : FlowDirection.LeftToRight;

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (Command is { } command && command.CanExecute(null))
            {
                command.Execute(null);

                Opacity = 0.5;
                await Task.Delay(90);
                Opacity = 1;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HudIconButton tap failed: {ex}");
        }
    }
}
