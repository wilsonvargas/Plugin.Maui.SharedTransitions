namespace Plugin.Maui.SharedTransitions;

/// <summary>
/// Attached properties that mark a view as a shared element between two pages.
/// </summary>
/// <remarks>
/// Views with the same <c>Name</c> on the source and destination pages are animated from one
/// position to the other. Inside lists, set <c>Group</c> (for example the item id) and pass the
/// same value when navigating, so only the tapped item takes part in the transition.
/// </remarks>
public static class SharedTransition
{
	/// <summary>Identifies the shared element across pages.</summary>
	public static readonly BindableProperty NameProperty =
		BindableProperty.CreateAttached("Name", typeof(string), typeof(SharedTransition), null);

	/// <summary>Disambiguates elements with the same name, typically inside a CollectionView.</summary>
	public static readonly BindableProperty GroupProperty =
		BindableProperty.CreateAttached("Group", typeof(object), typeof(SharedTransition), null);

	/// <summary>Gets the shared element name.</summary>
	public static string? GetName(BindableObject view) => (string?)view.GetValue(NameProperty);

	/// <summary>Sets the shared element name.</summary>
	public static void SetName(BindableObject view, string? value) => view.SetValue(NameProperty, value);

	/// <summary>Gets the shared element group.</summary>
	public static object? GetGroup(BindableObject view) => view.GetValue(GroupProperty);

	/// <summary>Sets the shared element group.</summary>
	public static void SetGroup(BindableObject view, object? value) => view.SetValue(GroupProperty, value);
}
