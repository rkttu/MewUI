using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Panels;

/// <summary>
/// A panel that arranges children in a grid with equal-sized cells.
/// </summary>
public class UniformGrid : Panel
{
    /// <summary>
    /// Gets or sets the number of rows. 0 means auto-calculate.
    /// </summary>
    public int Rows
    {
        get;
        set { field = Math.Max(0, value); InvalidateMeasure(); }
    }

    /// <summary>
    /// Gets or sets the number of columns. 0 means auto-calculate.
    /// </summary>
    public int Columns
    {
        get;
        set { field = Math.Max(0, value); InvalidateMeasure(); }
    }

    private (int rows, int columns) CalculateGridSize()
    {
        int count = Children.Count;
        if (count == 0) return (0, 0);

        int rows = Rows;
        int columns = Columns;

        if (rows == 0 && columns == 0)
        {
            // Auto-calculate both: make it roughly square
            columns = (int)Math.Ceiling(Math.Sqrt(count));
            rows = (int)Math.Ceiling((double)count / columns);
        }
        else if (rows == 0)
        {
            rows = (int)Math.Ceiling((double)count / columns);
        }
        else if (columns == 0)
        {
            columns = (int)Math.Ceiling((double)count / rows);
        }

        return (rows, columns);
    }

    protected override Size MeasureContent(Size availableSize)
    {
        var paddedSize = availableSize.Deflate(Padding);
        var (rows, columns) = CalculateGridSize();

        if (rows == 0 || columns == 0)
            return Size.Empty;

        double cellWidth = paddedSize.Width / columns;
        double cellHeight = paddedSize.Height / rows;
        var cellSize = new Size(cellWidth, cellHeight);

        double maxChildWidth = 0;
        double maxChildHeight = 0;

        foreach (var child in Children)
        {
            child.Measure(cellSize);
            maxChildWidth = Math.Max(maxChildWidth, child.DesiredSize.Width);
            maxChildHeight = Math.Max(maxChildHeight, child.DesiredSize.Height);
        }

        return new Size(maxChildWidth * columns, maxChildHeight * rows).Inflate(Padding);
    }

    protected override void ArrangeContent(Rect bounds)
    {
        var contentBounds = bounds.Deflate(Padding);
        var (rows, columns) = CalculateGridSize();

        if (rows == 0 || columns == 0)
            return;

        double cellWidth = contentBounds.Width / columns;
        double cellHeight = contentBounds.Height / rows;

        int index = 0;
        for (int row = 0; row < rows && index < Children.Count; row++)
        {
            for (int col = 0; col < columns && index < Children.Count; col++)
            {
                var child = Children[index++];
                child.Arrange(new Rect(
                    contentBounds.X + col * cellWidth,
                    contentBounds.Y + row * cellHeight,
                    cellWidth,
                    cellHeight));
            }
        }
    }
}
