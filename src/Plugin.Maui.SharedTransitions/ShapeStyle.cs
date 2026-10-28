using Microsoft.Maui.Controls.Shapes;

namespace Plugin.Maui.SharedTransitions;

/// <summary>
/// Visual state of a <see cref="Border"/> that can be drawn natively and interpolated,
/// so corner radius and colors morph smoothly instead of crossfading two stretched snapshots.
/// Corner radius and stroke thickness are in device-independent units.
/// </summary>
internal readonly record struct ShapeStyle(CornerRadius CornerRadius, Color Fill, Color? Stroke, double StrokeThickness)
{
	/// <summary>
	/// Reads the style of a border with a solid background and a rectangular or rounded rectangle shape;
	/// returns <c>null</c> for anything else (gradients, other shapes, dashed strokes), which falls back to snapshots.
	/// </summary>
	public static ShapeStyle? From(VisualElement element)
	{
		if (element is not Border border || border.StrokeDashArray is { Count: > 0 })
			return null;

		CornerRadius cornerRadius;
		switch (border.StrokeShape)
		{
			case null:
			case Rectangle:
				cornerRadius = default;
				break;
			case RoundRectangle roundRectangle:
				cornerRadius = roundRectangle.CornerRadius;
				break;
			default:
				return null;
		}

		// IView.Background is what the platform renders: Background, or BackgroundColor when it's empty.
		Color fill;
		switch (((IView)border).Background)
		{
			case null:
				fill = Colors.Transparent;
				break;
			case SolidPaint solid:
				fill = solid.Color ?? Colors.Transparent;
				break;
			default:
				return null;
		}

		Color? stroke;
		if (Brush.IsNullOrEmpty(border.Stroke))
			stroke = null;
		else if (border.Stroke is SolidColorBrush solidStroke)
			stroke = solidStroke.Color;
		else
			return null;

		var strokeThickness = stroke is null ? 0 : border.StrokeThickness;
		return new ShapeStyle(cornerRadius, fill, stroke, strokeThickness);
	}

	public static ShapeStyle Lerp(ShapeStyle from, ShapeStyle to, double t)
	{
		// A missing stroke fades from/to the other stroke's color at zero thickness.
		var fromStroke = from.Stroke ?? to.Stroke?.WithAlpha(0);
		var toStroke = to.Stroke ?? from.Stroke?.WithAlpha(0);

		return new ShapeStyle(
			new CornerRadius(
				Lerp(from.CornerRadius.TopLeft, to.CornerRadius.TopLeft, t),
				Lerp(from.CornerRadius.TopRight, to.CornerRadius.TopRight, t),
				Lerp(from.CornerRadius.BottomLeft, to.CornerRadius.BottomLeft, t),
				Lerp(from.CornerRadius.BottomRight, to.CornerRadius.BottomRight, t)),
			Lerp(from.Fill, to.Fill, t),
			fromStroke is null || toStroke is null ? null : Lerp(fromStroke, toStroke, t),
			Lerp(from.StrokeThickness, to.StrokeThickness, t));
	}

	static double Lerp(double from, double to, double t) => from + (to - from) * t;

	static Color Lerp(Color from, Color to, double t) => new(
		(float)Lerp(from.Red, to.Red, t),
		(float)Lerp(from.Green, to.Green, t),
		(float)Lerp(from.Blue, to.Blue, t),
		(float)Lerp(from.Alpha, to.Alpha, t));
}
