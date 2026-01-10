using Aprillz.MewUI.Binding;
using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Controls;

public abstract class TextBase : Control
{
    private ValueBinding<string>? _textBinding;
    private bool _suppressBindingSet;

    protected int _selectionStart;
    protected int _selectionLength;

    public string Text
    {
        get;
        set
        {
            var normalized = NormalizeText(value ?? string.Empty);
            if (field == normalized)
                return;

            var old = field;
            field = normalized;

            CaretPosition = Math.Min(CaretPosition, field.Length);
            _selectionStart = 0;
            _selectionLength = 0;

            OnTextChanged(old, field);
            TextChanged?.Invoke(field);
        }
    } = string.Empty;

    public string Placeholder
    {
        get;
        set { field = value ?? string.Empty; InvalidateVisual(); }
    } = string.Empty;

    public bool IsReadOnly
    {
        get;
        set { field = value; InvalidateVisual(); }
    }

    public bool AcceptTab { get; set; }

    public int CaretPosition
    {
        get;
        set { field = Math.Clamp(value, 0, Text.Length); InvalidateVisual(); }
    }

    public Action<string>? TextChanged { get; set; }

    public override bool Focusable => true;

    protected virtual string NormalizeText(string text) => text ?? string.Empty;

    protected virtual void OnTextChanged(string oldText, string newText)
    {
        InvalidateVisual();
    }

    public void SetTextBinding(
        Func<string> get,
        Action<string> set,
        Action<Action>? subscribe = null,
        Action<Action>? unsubscribe = null)
    {
        _textBinding?.Dispose();
        _textBinding = new ValueBinding<string>(
            get,
            set,
            subscribe,
            unsubscribe,
            onSourceChanged: () =>
            {
                if (IsFocused)
                    return;

                var value = NormalizeText(get() ?? string.Empty);
                if (Text == value)
                    return;

                _suppressBindingSet = true;
                try { Text = value; }
                finally { _suppressBindingSet = false; }
            });

        var existing = TextChanged;
        TextChanged = text =>
        {
            existing?.Invoke(text);

            if (_suppressBindingSet)
                return;

            _textBinding?.Set(text);
        };

        _suppressBindingSet = true;
        try { Text = NormalizeText(get() ?? string.Empty); }
        finally { _suppressBindingSet = false; }
    }

    protected override void OnDispose()
    {
        _textBinding?.Dispose();
        _textBinding = null;
    }
}

