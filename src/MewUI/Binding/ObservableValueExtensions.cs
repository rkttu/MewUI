namespace Aprillz.MewUI.Binding;

public static class ObservableValueExtensions
{
    public static ObservableValue<TResult> Select<TSource, TResult>(
        this ObservableValue<TSource> source,
        Func<TSource, TResult> selector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        var mapped = new ObservableValue<TResult>(selector(source.Value));

        source.Changed += () =>
        {
            mapped.Value = selector(source.Value);
        };

        return mapped;
    }
}

