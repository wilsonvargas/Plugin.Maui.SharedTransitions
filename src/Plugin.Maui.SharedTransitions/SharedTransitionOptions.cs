namespace Plugin.Maui.SharedTransitions;

/// <summary>
/// Options that control how a shared transition is animated.
/// </summary>
public sealed class SharedTransitionOptions
{
	/// <summary>Options used when none are passed to a navigation call.</summary>
	public static SharedTransitionOptions Default { get; internal set; } = new();

	/// <summary>Animation length in milliseconds. Default: 300.</summary>
	public uint Duration { get; set; } = 300;

	/// <summary>Easing applied to the movement and to the fades. Default: <see cref="Easing.CubicInOut"/>.</summary>
	public Easing Easing { get; set; } = Easing.CubicInOut;

	/// <summary>Maximum time to wait for the destination page to be laid out. Default: 600 ms.</summary>
	public TimeSpan LayoutTimeout { get; set; } = TimeSpan.FromMilliseconds(600);
}
