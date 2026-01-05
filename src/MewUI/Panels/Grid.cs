using Aprillz.MewUI.Elements;
using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Panels;

/// <summary>
/// Grid unit type for row/column sizing.
/// </summary>
public enum GridUnitType
{
    /// <summary>Size to content.</summary>
    Auto,
    /// <summary>Fixed pixel size.</summary>
    Pixel,
    /// <summary>Proportional size (star sizing).</summary>
    Star
}

/// <summary>
/// Represents a grid length value.
/// </summary>
public readonly struct GridLength
{
    public double Value { get; }
    public GridUnitType GridUnitType { get; }

    public GridLength(double value, GridUnitType type = GridUnitType.Pixel)
    {
        Value = value;
        GridUnitType = type;
    }

    public bool IsAuto => GridUnitType == GridUnitType.Auto;
    public bool IsStar => GridUnitType == GridUnitType.Star;
    public bool IsAbsolute => GridUnitType == GridUnitType.Pixel;

    public static GridLength Auto => new(1, GridUnitType.Auto);
    public static GridLength Star => new(1, GridUnitType.Star);
    public static GridLength Stars(double value) => new(value, GridUnitType.Star);
    public static GridLength Pixels(double value) => new(value, GridUnitType.Pixel);

    public static implicit operator GridLength(double value) => new(value, GridUnitType.Pixel);
}

/// <summary>
/// Defines a row in a Grid.
/// </summary>
public class RowDefinition
{
    public GridLength Height { get; set; } = GridLength.Star;
    public double MinHeight { get; set; }
    public double MaxHeight { get; set; } = double.PositiveInfinity;
    internal double ActualHeight { get; set; }
    internal double Offset { get; set; }
}

/// <summary>
/// Defines a column in a Grid.
/// </summary>
public class ColumnDefinition
{
    public GridLength Width { get; set; } = GridLength.Star;
    public double MinWidth { get; set; }
    public double MaxWidth { get; set; } = double.PositiveInfinity;
    internal double ActualWidth { get; set; }
    internal double Offset { get; set; }
}

/// <summary>
/// A panel that arranges children in a grid of rows and columns.
/// </summary>
public class Grid : Panel
{
    private readonly List<RowDefinition> _rowDefinitions = new();
    private readonly List<ColumnDefinition> _columnDefinitions = new();

    // Attached properties storage (AOT-compatible)
    private static readonly Dictionary<Element, int> _rowProperty = new();
    private static readonly Dictionary<Element, int> _columnProperty = new();
    private static readonly Dictionary<Element, int> _rowSpanProperty = new();
    private static readonly Dictionary<Element, int> _columnSpanProperty = new();

    public IList<RowDefinition> RowDefinitions => _rowDefinitions;
    public IList<ColumnDefinition> ColumnDefinitions => _columnDefinitions;

    /// <summary>
    /// When set, applies this margin to newly added children that do not already have a margin.
    /// A margin is considered "not set" when it is <see cref="Thickness.Zero"/>.
    /// </summary>
    public Thickness ChildMargin
    {
        get;
        set
        {
            field = value;
            ApplyChildMarginToExistingChildren();
        }
    } = Thickness.Zero;

    #region Attached Properties

    public static void SetRow(Element element, int row) => _rowProperty[element] = row;
    public static int GetRow(Element element) => _rowProperty.GetValueOrDefault(element, 0);

    public static void SetColumn(Element element, int column) => _columnProperty[element] = column;
    public static int GetColumn(Element element) => _columnProperty.GetValueOrDefault(element, 0);

    public static void SetRowSpan(Element element, int span) => _rowSpanProperty[element] = span;
    public static int GetRowSpan(Element element) => _rowSpanProperty.GetValueOrDefault(element, 1);

    public static void SetColumnSpan(Element element, int span) => _columnSpanProperty[element] = span;
    public static int GetColumnSpan(Element element) => _columnSpanProperty.GetValueOrDefault(element, 1);

    #endregion

    protected override void OnChildRemoved(Element child)
    {
        _rowProperty.Remove(child);
        _columnProperty.Remove(child);
        _rowSpanProperty.Remove(child);
        _columnSpanProperty.Remove(child);
    }

    protected override void OnChildAdded(Element child)
    {
        base.OnChildAdded(child);
        ApplyChildMargin(child);
    }

    protected override Size MeasureContent(Size availableSize)
    {
        var paddedSize = availableSize.Deflate(Padding);
        EnsureDefinitions();

        int rowCount = _rowDefinitions.Count;
        int colCount = _columnDefinitions.Count;

        // First pass: measure children with infinite size to get desired sizes
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
        }

        // Calculate column widths
        CalculateLengths(_columnDefinitions, paddedSize.Width, true);

        // Calculate row heights
        CalculateLengths(_rowDefinitions, paddedSize.Height, false);

        // Calculate total size
        double totalWidth = 0;
        double totalHeight = 0;

        foreach (var col in _columnDefinitions)
            totalWidth += col.ActualWidth;
        foreach (var row in _rowDefinitions)
            totalHeight += row.ActualHeight;

        return new Size(totalWidth, totalHeight).Inflate(Padding);
    }

    private void ApplyChildMarginToExistingChildren()
    {
        if (ChildMargin == Thickness.Zero)
            return;

        foreach (var child in Children)
            ApplyChildMargin(child);
    }

    private void ApplyChildMargin(Element child)
    {
        if (ChildMargin == Thickness.Zero)
            return;

        if (child is FrameworkElement frameworkElement && frameworkElement.Margin == Thickness.Zero)
            frameworkElement.Margin = ChildMargin;
    }

    protected override void ArrangeContent(Rect bounds)
    {
        var contentBounds = bounds.Deflate(Padding);
        EnsureDefinitions();

        // Recalculate with actual available space
        CalculateLengths(_columnDefinitions, contentBounds.Width, true);
        CalculateLengths(_rowDefinitions, contentBounds.Height, false);

        // Calculate offsets
        CalculateOffsets(_columnDefinitions);
        CalculateOffsets(_rowDefinitions);

        // Arrange children
        foreach (var child in Children)
        {
            int row = GetRow(child);
            int col = GetColumn(child);
            int rowSpan = GetRowSpan(child);
            int colSpan = GetColumnSpan(child);

            // Clamp to valid range
            row = Math.Clamp(row, 0, _rowDefinitions.Count - 1);
            col = Math.Clamp(col, 0, _columnDefinitions.Count - 1);
            rowSpan = Math.Clamp(rowSpan, 1, _rowDefinitions.Count - row);
            colSpan = Math.Clamp(colSpan, 1, _columnDefinitions.Count - col);

            // Calculate cell bounds
            double x = contentBounds.X + _columnDefinitions[col].Offset;
            double y = contentBounds.Y + _rowDefinitions[row].Offset;

            double width = 0;
            for (int i = col; i < col + colSpan; i++)
                width += _columnDefinitions[i].ActualWidth;

            double height = 0;
            for (int i = row; i < row + rowSpan; i++)
                height += _rowDefinitions[i].ActualHeight;

            child.Arrange(new Rect(x, y, width, height));
        }
    }

    private void EnsureDefinitions()
    {
        if (_rowDefinitions.Count == 0)
            _rowDefinitions.Add(new RowDefinition());
        if (_columnDefinitions.Count == 0)
            _columnDefinitions.Add(new ColumnDefinition());
    }

    private void CalculateLengths<T>(IList<T> definitions, double available, bool isColumn) where T : class
    {
        bool isInfinite = double.IsPositiveInfinity(available);
        double totalFixed = 0;
        double totalStars = 0;

        foreach (var def in definitions)
        {
            var length = isColumn ? ((ColumnDefinition)(object)def).Width : ((RowDefinition)(object)def).Height;
            var min = isColumn ? ((ColumnDefinition)(object)def).MinWidth : ((RowDefinition)(object)def).MinHeight;
            var max = isColumn ? ((ColumnDefinition)(object)def).MaxWidth : ((RowDefinition)(object)def).MaxHeight;

            if (length.IsAbsolute)
            {
                var size = Math.Clamp(length.Value, min, max);
                SetActualSize(def, size, isColumn);
                totalFixed += size;
            }
            else if (length.IsAuto)
            {
                // Find max desired size of children in this row/column
                double maxDesired = 0;
                foreach (var child in Children)
                {
                    int index = isColumn ? GetColumn(child) : GetRow(child);
                    int span = isColumn ? GetColumnSpan(child) : GetRowSpan(child);
                    int defIndex = definitions.IndexOf(def);

                    if (index <= defIndex && index + span > defIndex)
                    {
                        double desired = isColumn ? child.DesiredSize.Width : child.DesiredSize.Height;
                        maxDesired = Math.Max(maxDesired, desired / span);
                    }
                }
                var size = Math.Clamp(maxDesired, min, max);
                SetActualSize(def, size, isColumn);
                totalFixed += size;
            }
            else // Star
            {
                if (isInfinite)
                {
                    // WPF-like behavior: when unconstrained, star sizing behaves like "size to content" in Measure.
                    // Actual star distribution still happens during Arrange with the real available size.
                    double maxDesired = 0;
                    foreach (var child in Children)
                    {
                        int index = isColumn ? GetColumn(child) : GetRow(child);
                        int span = isColumn ? GetColumnSpan(child) : GetRowSpan(child);
                        int defIndex = definitions.IndexOf(def);

                        if (index <= defIndex && index + span > defIndex)
                        {
                            double desired = isColumn ? child.DesiredSize.Width : child.DesiredSize.Height;
                            maxDesired = Math.Max(maxDesired, desired / span);
                        }
                    }

                    var size = Math.Clamp(maxDesired, min, max);
                    SetActualSize(def, size, isColumn);
                    totalFixed += size;
                }
                else
                {
                    totalStars += length.Value;
                }
            }
        }

        // Distribute remaining space to star-sized definitions
        double remaining = Math.Max(0, available - totalFixed);

        if (totalStars > 0)
        {
            foreach (var def in definitions)
            {
                var length = isColumn ? ((ColumnDefinition)(object)def).Width : ((RowDefinition)(object)def).Height;
                var min = isColumn ? ((ColumnDefinition)(object)def).MinWidth : ((RowDefinition)(object)def).MinHeight;
                var max = isColumn ? ((ColumnDefinition)(object)def).MaxWidth : ((RowDefinition)(object)def).MaxHeight;

                if (length.IsStar)
                {
                    var size = Math.Clamp(remaining * length.Value / totalStars, min, max);
                    SetActualSize(def, size, isColumn);
                }
            }
        }
    }

    private static void SetActualSize<T>(T def, double size, bool isColumn) where T : class
    {
        if (isColumn)
            ((ColumnDefinition)(object)def).ActualWidth = size;
        else
            ((RowDefinition)(object)def).ActualHeight = size;
    }

    private static void CalculateOffsets<T>(IList<T> definitions) where T : class
    {
        double offset = 0;
        foreach (var def in definitions)
        {
            if (def is ColumnDefinition col)
            {
                col.Offset = offset;
                offset += col.ActualWidth;
            }
            else if (def is RowDefinition row)
            {
                row.Offset = offset;
                offset += row.ActualHeight;
            }
        }
    }
}
