namespace Plugin.Maui.SharedTransitions;

/// <summary>
/// Native overlay placed above the whole window while a transition runs.
/// All rectangles are in platform units relative to the overlay.
/// </summary>
internal sealed partial class TransitionScene : IDisposable
{
	/// <summary>Creates the overlay, or returns <c>null</c> when the platform is not supported.</summary>
	public static partial TransitionScene? TryCreate(VisualElement anchor);

	/// <summary>Current frame of an element, or <c>null</c> while it is not laid out.</summary>
	public partial Rect? GetFrame(VisualElement element);

	/// <summary>Adds a snapshot of the element on top of the overlay, at its current frame.</summary>
	public partial SnapshotLayer? Capture(VisualElement element);

	/// <summary>Adds a natively drawn shape on top of the overlay.</summary>
	public partial ShapeLayer AddShape(Rect frame, ShapeStyle style);

	/// <summary>Adds a snapshot of the whole window at the bottom of the overlay.</summary>
	public partial SnapshotLayer? CaptureWindow();

	/// <summary>
	/// Hides the scroll bars of a page until the scene is disposed. Android shows them for a moment
	/// when a scrolling view is attached again, which would flash on top of the transition.
	/// </summary>
	public void HideScrollBars(VisualElement page) => HideScrollBarsCore(page);

	partial void HideScrollBarsCore(VisualElement page);

	/// <summary>Removes the overlay and releases the snapshots.</summary>
	public partial void Dispose();
}

/// <summary>
/// A snapshot shown in the overlay.
/// </summary>
internal sealed partial class SnapshotLayer
{
	/// <summary>Frame where the snapshot was captured.</summary>
	public Rect Frame { get; }

	/// <summary>Moves and scales the snapshot to <paramref name="frame"/> with the given opacity.</summary>
	public partial void Apply(Rect frame, double opacity);
}

/// <summary>
/// A natively drawn border shown in the overlay.
/// </summary>
internal sealed partial class ShapeLayer
{
	/// <summary>Draws the shape at <paramref name="frame"/> with the given style and opacity.</summary>
	public partial void Apply(Rect frame, ShapeStyle style, double opacity);
}
