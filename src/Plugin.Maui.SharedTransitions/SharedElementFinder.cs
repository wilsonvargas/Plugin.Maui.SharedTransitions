namespace Plugin.Maui.SharedTransitions;

internal static class SharedElementFinder
{
	/// <summary>
	/// Returns the visible shared elements of a page in post-order (children before parents),
	/// so nested elements can be hidden before their parent is captured.
	/// </summary>
	public static List<(string Name, VisualElement Element)> Find(Page page, object? group)
	{
		var result = new List<(string, VisualElement)>();
		var names = new HashSet<string>(StringComparer.Ordinal);
		Visit(page, group, result, names);
		return result;
	}

	static void Visit(IVisualTreeElement node, object? group, List<(string, VisualElement)> result, HashSet<string> names)
	{
		if (node is VisualElement { IsVisible: false })
			return;

		foreach (var child in node.GetVisualChildren())
			Visit(child, group, result, names);

		if (node is VisualElement element
			&& SharedTransition.GetName(element) is { Length: > 0 } name
			&& GroupMatches(SharedTransition.GetGroup(element), group)
			&& names.Add(name))
		{
			result.Add((name, element));
		}
	}

	// Elements without group always take part; grouped ones only when they match the selected group.
	static bool GroupMatches(object? elementGroup, object? selectedGroup) =>
		elementGroup is null
		|| Equals(elementGroup, selectedGroup)
		|| (selectedGroup is not null && string.Equals(elementGroup.ToString(), selectedGroup.ToString(), StringComparison.Ordinal));
}
