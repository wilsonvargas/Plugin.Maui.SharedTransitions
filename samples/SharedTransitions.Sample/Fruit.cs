namespace SharedTransitions.Sample;

public sealed record Fruit(int Id, string Name, string Category, string Emoji, Color Color, string Description)
{
	public static IReadOnlyList<Fruit> All { get; } =
	[
		new(1, "Strawberry", "Berry", "🍓", Color.FromArgb("#FFCDD2"), "Sweet and juicy, and the only fruit that wears its seeds on the outside."),
		new(2, "Blueberry", "Berry", "🫐", Color.FromArgb("#C5CAE9"), "Small, round and packed with antioxidants. Great in pancakes and smoothies."),
		new(3, "Orange", "Citrus", "🍊", Color.FromArgb("#FFE0B2"), "Bright and refreshing, a classic source of vitamin C for breakfast."),
		new(4, "Kiwi", "Tropical", "🥝", Color.FromArgb("#DCEDC8"), "Fuzzy brown outside, vivid green inside, with a sweet and tangy taste."),
		new(5, "Grapes", "Berry", "🍇", Color.FromArgb("#E1BEE7"), "Grown in clusters, eaten fresh, dried into raisins or turned into juice."),
		new(6, "Lemon", "Citrus", "🍋", Color.FromArgb("#FFF9C4"), "Sour and aromatic, the secret ingredient of countless drinks and desserts."),
		new(7, "Coconut", "Tropical", "🥥", Color.FromArgb("#D7CCC8"), "A hard shell with refreshing water and creamy white flesh inside."),
		new(8, "Watermelon", "Melon", "🍉", Color.FromArgb("#B2DFDB"), "More than 90% water: the perfect fruit for a hot summer day."),
	];
}
