namespace Plugin.Maui.SharedTransitions;

/// <summary>
/// Options that control how a shared transition is animated.
/// </summary>
public sealed class SharedTransitionOptions
{
	/// <summary>Options used when none are passed to a navigation call.</summary>
	public static SharedTransitionOptions Default { get; internal set; } = new();

	/// <summary>Animation length in milliseconds. Default: 250.</summary>
	public uint Duration { get; set; } = 250;

	/// <summary>Easing applied to the movement and to the fades. Default: <see cref="Easing.CubicOut"/>, which starts fast so the transition responds right away to the tap.</summary>
	public Easing Easing { get; set; } = Easing.CubicOut;

	/// <summary>How the rest of the page changes while the shared elements fly. Default: <see cref="SharedTransitionPageAnimation.Fade"/>.</summary>
	public SharedTransitionPageAnimation PageAnimation { get; set; } = SharedTransitionPageAnimation.Fade;

	/// <summary>Path followed by the shared elements. Default: <see cref="SharedTransitionPath.Linear"/>.</summary>
	public SharedTransitionPath Path { get; set; } = SharedTransitionPath.Linear;

	/// <summary>Maximum time to wait for the destination page to be laid out. Default: 600 ms.</summary>
	public TimeSpan LayoutTimeout { get; set; } = TimeSpan.FromMilliseconds(600);

	/// <summary>
	/// Returns a copy of these options with some changes, e.g.
	/// <c>SharedTransitionOptions.Default.With(o => o.PageAnimation = SharedTransitionPageAnimation.Slide)</c>.
	/// </summary>
	public SharedTransitionOptions With(Action<SharedTransitionOptions> change)
	{
		ArgumentNullException.ThrowIfNull(change);

		var copy = (SharedTransitionOptions)MemberwiseClone();
		change(copy);
		return copy;
	}
}

/// <summary>
/// How the rest of the page changes while the shared elements fly.
/// </summary>
public enum SharedTransitionPageAnimation
{
	/// <summary>The previous page fades out over the new one.</summary>
	Fade,

	/// <summary>The new page slides in from the trailing edge (and back out on pop), like a regular push.</summary>
	Slide,

	/// <summary>The pages zoom and crossfade: forward when pushing, backward when popping.</summary>
	Scale,

	/// <summary>
	/// The rest of the page changes right away and only the shared elements are animated.
	/// To navigate without any animation, use the regular MAUI navigation with <c>animated: false</c>.
	/// </summary>
	SharedElementsOnly,
}

/// <summary>
/// Path followed by the shared elements between both pages.
/// </summary>
public enum SharedTransitionPath
{
	/// <summary>Straight line.</summary>
	Linear,

	/// <summary>Curved path that moves horizontally first, which looks more natural for diagonal movements.</summary>
	Arc,
}
