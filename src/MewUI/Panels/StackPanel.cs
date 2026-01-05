using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Panels;

/// <summary>
/// Orientation for layout panels.
/// </summary>
public enum Orientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// A panel that arranges children in a stack.
/// </summary>
public class StackPanel : Panel
{
    /// <summary>
    /// Gets or sets the orientation of the stack.
    /// </summary>
    public Orientation Orientation
    {
        get;
        set { field = value; InvalidateMeasure(); }
    } = Orientation.Vertical;

    /// <summary>
    /// Gets or sets the spacing between children.
    /// </summary>
    public double Spacing
    {
        get;
        set { field = value; InvalidateMeasure(); }
    }

    protected override Size MeasureContent(Size availableSize)
    {
        double totalMain = 0;
        double maxCross = 0;

        var paddedSize = availableSize.Deflate(Padding);

        foreach (var child in Children)
        {
            if (Orientation == Orientation.Vertical)
            {
                child.Measure(new Size(paddedSize.Width, double.PositiveInfinity));
                totalMain += child.DesiredSize.Height;
                maxCross = Math.Max(maxCross, child.DesiredSize.Width);
            }
            else
            {
                child.Measure(new Size(double.PositiveInfinity, paddedSize.Height));
                totalMain += child.DesiredSize.Width;
                maxCross = Math.Max(maxCross, child.DesiredSize.Height);
            }
        }

        // Add spacing
        if (Children.Count > 1)
            totalMain += (Children.Count - 1) * Spacing;

        var contentSize = Orientation == Orientation.Vertical
            ? new Size(maxCross, totalMain)
            : new Size(totalMain, maxCross);

        return contentSize.Inflate(Padding);
    }

    protected override void ArrangeContent(Rect bounds)
    {
        var contentBounds = bounds.Deflate(Padding);
        double offset = 0;

        foreach (var child in Children)
        {
            if (Orientation == Orientation.Vertical)
            {
                var childHeight = child.DesiredSize.Height;
                child.Arrange(new Rect(
                    contentBounds.X,
                    contentBounds.Y + offset,
                    contentBounds.Width,
                    childHeight));
                offset += childHeight + Spacing;
            }
            else
            {
                var childWidth = child.DesiredSize.Width;
                child.Arrange(new Rect(
                    contentBounds.X + offset,
                    contentBounds.Y,
                    childWidth,
                    contentBounds.Height));
                offset += childWidth + Spacing;
            }
        }
    }
}
