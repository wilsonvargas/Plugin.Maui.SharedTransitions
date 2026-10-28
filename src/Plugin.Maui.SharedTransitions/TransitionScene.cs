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

	/// <summary>Adds a snapshot of the whole window at the bottom of the overlay.</summary>
	public partial SnapshotLayer? CaptureWindow();

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
