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

	/// <summary>Maximum time to wait for the destination page to be laid out. Default: 600 ms.</summary>
	public TimeSpan LayoutTimeout { get; set; } = TimeSpan.FromMilliseconds(600);
}
