using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Player.App.Features.Explorer;

/// <summary>
/// A long press on a row or tile, which MAUI has no recognizer for. On Android the native view gets the
/// <c>LongClick</c>; the row's tap recognizer and the ⋮ button keep their own touch handling.
/// <see cref="Command"/> runs with <see cref="CommandParameter"/> when the long press happens.
/// </summary>
public sealed class LongPressBehavior : Behavior
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(LongPressBehavior));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(LongPressBehavior));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
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
        native.LongClickable = true;
        native.LongClick += OnLongClick;
    }

    private void Detach()
    {
        if (_native is null)
        {
            return;
        }

        _native.LongClick -= OnLongClick;
        _native.LongClickable = false;
        _native = null;
    }

    private void OnLongClick(object? sender, EventArgs e)
    {
        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }
#endif
}
