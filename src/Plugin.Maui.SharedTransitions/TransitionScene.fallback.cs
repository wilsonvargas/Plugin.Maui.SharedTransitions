namespace Plugin.Maui.SharedTransitions;

// Platforms without an overlay implementation yet (Windows, plain net10.0): navigation falls back to the default animation.
internal sealed partial class TransitionScene
{
	public static partial TransitionScene? TryCreate(VisualElement anchor) => null;

	public partial Rect? GetFrame(VisualElement element) => null;

	public partial SnapshotLayer? Capture(VisualElement element) => null;

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
}
