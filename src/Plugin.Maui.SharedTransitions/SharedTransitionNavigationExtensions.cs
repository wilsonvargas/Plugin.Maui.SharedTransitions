using System.Runtime.CompilerServices;

namespace Plugin.Maui.SharedTransitions;

/// <summary>
/// Navigation methods that animate the shared elements between pages.
/// Works with Shell and NavigationPage; unsupported platforms use the default animation.
/// </summary>
public static class SharedTransitionNavigationExtensions
{
	sealed record PushRecord(WeakReference<Page> Source, object? Group);

	static readonly ConditionalWeakTable<Page, PushRecord> Records = new();

	/// <summary>
	/// Pushes <paramref name="destination"/> animating the shared elements of <paramref name="source"/>.
	/// </summary>
	/// <param name="source">The page currently visible.</param>
	/// <param name="destination">The page to push.</param>
	/// <param name="group">Selected group: only grouped elements with this value take part.</param>
	/// <param name="options">Animation options; <see cref="SharedTransitionOptions.Default"/> when null.</param>
	public static Task PushWithSharedTransitionAsync(this Page source, Page destination, object? group = null, SharedTransitionOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);

		Records.AddOrUpdate(destination, new PushRecord(new WeakReference<Page>(source), group));

		return SharedTransitionRunner.RunAsync(
			source,
			destination,
			group,
			isPop: false,
			animated => source.Navigation.PushAsync(destination, animated),
			options ?? SharedTransitionOptions.Default);
	}

	/// <summary>
	/// Pops <paramref name="current"/> animating its shared elements back to the page that pushed it.
	/// </summary>
	/// <param name="current">The page currently visible, pushed with <see cref="PushWithSharedTransitionAsync"/>.</param>
	/// <param name="options">Animation options; <see cref="SharedTransitionOptions.Default"/> when null.</param>
	public static Task PopWithSharedTransitionAsync(this Page current, SharedTransitionOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(current);

		if (!Records.TryGetValue(current, out var record) || !record.Source.TryGetTarget(out var previous))
			return current.Navigation.PopAsync(true);

		Records.Remove(current);

		return SharedTransitionRunner.RunAsync(
			current,
			previous,
			record.Group,
			isPop: true,
			animated => current.Navigation.PopAsync(animated),
			options ?? SharedTransitionOptions.Default);
	}
}
