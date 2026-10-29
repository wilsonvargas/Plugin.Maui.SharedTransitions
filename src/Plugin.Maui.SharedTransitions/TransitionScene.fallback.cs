namespace Plugin.Maui.SharedTransitions;

// Platforms without an overlay implementation yet (Windows, plain net9.0/net10.0): navigation falls back to the default animation.
internal sealed partial class TransitionScene
{
	public static partial TransitionScene? TryCreate(VisualElement anchor) => null;

	public partial Rect? GetFrame(VisualElement element) => null;

	public partial SnapshotLayer? Capture(VisualElement element) => null;

	public partial ShapeLayer AddShape(Rect frame, ShapeStyle style) => new();

	public partial SnapshotLayer? CaptureWindow() => null;

	public partial void Dispose()
	{
	}
}

internal sealed partial class SnapshotLayer
{
	public partial void Apply(Rect frame, double opacity)
	{
	}

	public partial void BringToFront()
	{
	}

	public partial void Clip(Rect rect)
	{
	}

	public partial void Cache()
	{
	}
}

internal sealed partial class ShapeLayer
{
	public partial void Apply(Rect frame, ShapeStyle style, double opacity)
	{
	}

	public partial void BringToFront()
	{
	}
}
