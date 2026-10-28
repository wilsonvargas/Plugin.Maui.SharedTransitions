namespace Plugin.Maui.SharedTransitions;

/// <summary>
/// Registration of the plugin in <see cref="MauiAppBuilder"/>.
/// </summary>
public static class AppHostBuilderExtensions
{
	/// <summary>
	/// Enables shared transitions and optionally changes the default options.
	/// </summary>
	public static MauiAppBuilder UseSharedTransitions(this MauiAppBuilder builder, Action<SharedTransitionOptions>? configure = null)
	{
		var options = new SharedTransitionOptions();
		configure?.Invoke(options);
		SharedTransitionOptions.Default = options;
		return builder;
	}
}
