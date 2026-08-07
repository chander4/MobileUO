using System;

namespace Hollowmere.Hud
{
    public enum HudOrientation
    {
        Portrait,
        Landscape
    }

    public enum HudDeviceClass
    {
        Phone,
        Tablet,
        Desktop
    }

    public enum HudSizePolicy
    {
        Intrinsic,
        DiscreteScale,
        Resizable
    }

    public enum HudDockEdge
    {
        None,
        Left,
        Top,
        Right,
        Bottom
    }

    [Serializable]
    public struct HudPoint
    {
        public double X;
        public double Y;

        public HudPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public struct HudSize
    {
        public double Width;
        public double Height;

        public HudSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    [Serializable]
    public struct HudRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public HudRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    [Serializable]
    public struct HudInsets
    {
        public double Left;
        public double Top;
        public double Right;
        public double Bottom;

        public HudInsets(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    /// <summary>
    /// Persistent, renderer-neutral placement. Anchor and pivot are normalized to
    /// the safe rectangle; Offset is stored in logical HUD units, never pixels.
    /// </summary>
    [Serializable]
    public sealed class HudPlacement
    {
        public HudPoint Anchor = new HudPoint(0.5, 0.5);
        public HudPoint Pivot = new HudPoint(0.5, 0.5);
        public HudPoint LogicalOffset = new HudPoint(0, 0);
        public double Scale = 1;
        public HudDockEdge DockEdge = HudDockEdge.None;
        public int DockOrder;
    }

    public static class HudPlacementMath
    {
        public static HudRect CalculateSafeArea(HudSize viewport, HudInsets insets)
        {
            double width = Math.Max(0, viewport.Width - insets.Left - insets.Right);
            double height = Math.Max(0, viewport.Height - insets.Top - insets.Bottom);
            return new HudRect(insets.Left, insets.Top, width, height);
        }

        public static double LogicalUnitSize(HudRect safeArea)
        {
            return Math.Min(safeArea.Width, safeArea.Height) / 100d;
        }

        public static HudRect Resolve(HudPlacement placement, HudRect safeArea, HudSize intrinsicSize)
        {
            if (placement == null)
            {
                throw new ArgumentNullException(nameof(placement));
            }

            double unit = LogicalUnitSize(safeArea);
            double width = intrinsicSize.Width * placement.Scale;
            double height = intrinsicSize.Height * placement.Scale;
            double anchorX = safeArea.X + safeArea.Width * Clamp01(placement.Anchor.X);
            double anchorY = safeArea.Y + safeArea.Height * Clamp01(placement.Anchor.Y);

            return new HudRect(
                anchorX + placement.LogicalOffset.X * unit - width * Clamp01(placement.Pivot.X),
                anchorY + placement.LogicalOffset.Y * unit - height * Clamp01(placement.Pivot.Y),
                width,
                height);
        }

        public static HudPlacement TransformOrientation(HudPlacement source, HudOrientation from, HudOrientation to)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var result = new HudPlacement
            {
                Anchor = source.Anchor,
                Pivot = source.Pivot,
                LogicalOffset = source.LogicalOffset,
                Scale = source.Scale,
                DockEdge = source.DockEdge,
                DockOrder = source.DockOrder
            };

            if (from == to)
            {
                return result;
            }

            result.Anchor = new HudPoint(1d - source.Anchor.Y, source.Anchor.X);
            result.Pivot = new HudPoint(1d - source.Pivot.Y, source.Pivot.X);
            result.LogicalOffset = new HudPoint(-source.LogicalOffset.Y, source.LogicalOffset.X);
            result.DockEdge = RotateDockEdgeClockwise(source.DockEdge);
            return result;
        }

        public static HudRect SnapToSafeAreaEdges(HudRect rect, HudRect safeArea, double tolerance)
        {
            double x = rect.X;
            double y = rect.Y;

            if (Math.Abs(rect.X - safeArea.X) <= tolerance)
                x = safeArea.X;
            else if (Math.Abs((rect.X + rect.Width) - (safeArea.X + safeArea.Width)) <= tolerance)
                x = safeArea.X + safeArea.Width - rect.Width;

            if (Math.Abs(rect.Y - safeArea.Y) <= tolerance)
                y = safeArea.Y;
            else if (Math.Abs((rect.Y + rect.Height) - (safeArea.Y + safeArea.Height)) <= tolerance)
                y = safeArea.Y + safeArea.Height - rect.Height;

            return new HudRect(x, y, rect.Width, rect.Height);
        }

        public static HudRect ResolveDocked(HudRect safeArea, HudSize size, HudDockEdge edge, int order, double spacing)
        {
            double offset = Math.Max(0, order) * (edge == HudDockEdge.Left || edge == HudDockEdge.Right ? size.Height + spacing : size.Width + spacing);

            switch (edge)
            {
                case HudDockEdge.Left:
                    return new HudRect(safeArea.X, safeArea.Y + offset, size.Width, size.Height);
                case HudDockEdge.Top:
                    return new HudRect(safeArea.X + offset, safeArea.Y, size.Width, size.Height);
                case HudDockEdge.Right:
                    return new HudRect(safeArea.X + safeArea.Width - size.Width, safeArea.Y + offset, size.Width, size.Height);
                case HudDockEdge.Bottom:
                    return new HudRect(safeArea.X + offset, safeArea.Y + safeArea.Height - size.Height, size.Width, size.Height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), "A docked placement requires an edge.");
            }
        }

        private static HudDockEdge RotateDockEdgeClockwise(HudDockEdge edge)
        {
            switch (edge)
            {
                case HudDockEdge.Left: return HudDockEdge.Top;
                case HudDockEdge.Top: return HudDockEdge.Right;
                case HudDockEdge.Right: return HudDockEdge.Bottom;
                case HudDockEdge.Bottom: return HudDockEdge.Left;
                default: return HudDockEdge.None;
            }
        }

        private static double Clamp01(double value)
        {
            return Math.Max(0, Math.Min(1, value));
        }
    }
}
