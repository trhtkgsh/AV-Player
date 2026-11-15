using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Player.App.Features.Explorer;

/// <summary>
/// The tap and the long press of a row or tile. The row has no MAUI <c>TapGestureRecognizer</c>: MAUI's touch
/// listener would take the touch stream, so the native view never got its long press. On Android the native view
/// itself gets <c>Click</c> (runs <see cref="TapCommand"/>) and <c>LongClick</c> (runs <see cref="LongPressCommand"/>),
/// both with <see cref="CommandParameter"/>. Android sends no <c>Click</c> after a <c>LongClick</c>; scrolling cancels both.
/// </summary>
public sealed class LongPressBehavior : Behavior
{
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(LongPressBehavior));

    public static readonly BindableProperty LongPressCommandProperty =
        BindableProperty.Create(nameof(LongPressCommand), typeof(ICommand), typeof(LongPressBehavior));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(LongPressBehavior));

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public ICommand? LongPressCommand
    {
        get => (ICommand?)GetValue(LongPressCommandProperty);
        set => SetValue(LongPressCommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

#if ANDROID
    private Android.Views.View? _native;

    protected override void OnAttachedTo(BindableObject bindable)
    {
        base.OnAttachedTo(bindable);
        if (bindable is VisualElement element)
        {
            element.HandlerChanged += OnHandlerChanged;
            Attach(element);
        }
    }

    protected override void OnDetachingFrom(BindableObject bindable)
    {
        if (bindable is VisualElement element)
        {
            element.HandlerChanged -= OnHandlerChanged;
        }

        Detach();
        base.OnDetachingFrom(bindable);
    }

    // The native view is made again when the handler is: a recycled row gets a new one.
    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        Detach();
        if (sender is VisualElement element)
        {
            Attach(element);
        }
    }

    private void Attach(VisualElement element)
    {
        if (element.Handler?.PlatformView is not Android.Views.View native)
        {
            return;
        }

        _native = native;
        native.Clickable = true;
        native.LongClickable = true;
        native.Click += OnClick;
        native.LongClick += OnLongClick;
    }

    private void Detach()
    {
        if (_native is null)
        {
            return;
        }

        _native.Click -= OnClick;
        _native.LongClick -= OnLongClick;
        _native.LongClickable = false;
        _native.Clickable = false;
        _native = null;
    }

    private void OnClick(object? sender, EventArgs e) => Run(TapCommand);

    private void OnLongClick(object? sender, EventArgs e) => Run(LongPressCommand);

    private void Run(ICommand? command)
    {
        if (command is not null && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }
#endif
}
