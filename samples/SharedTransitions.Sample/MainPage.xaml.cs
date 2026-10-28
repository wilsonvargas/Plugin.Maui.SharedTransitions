using Plugin.Maui.SharedTransitions;

namespace SharedTransitions.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		FruitsView.ItemsSource = Fruit.All;

		PageAnimationPicker.ItemsSource = PageAnimations.Select(o => o.Label).ToList();
		PageAnimationPicker.SelectedIndex = Array.FindIndex(PageAnimations, o => o.Value == SharedTransitionOptions.Default.PageAnimation);
		PathPicker.ItemsSource = Paths.Select(o => o.Label).ToList();
		PathPicker.SelectedIndex = Array.FindIndex(Paths, o => o.Value == SharedTransitionOptions.Default.Path);
	}

	static readonly (string Label, SharedTransitionPageAnimation Value)[] PageAnimations =
	[
		("Fade", SharedTransitionPageAnimation.Fade),
		("Slide", SharedTransitionPageAnimation.Slide),
		("Scale", SharedTransitionPageAnimation.Scale),
	];

	static readonly (string Label, SharedTransitionPath Value)[] Paths =
	[
		("Linear", SharedTransitionPath.Linear),
		("Arc", SharedTransitionPath.Arc),
	];

	// Options used by both pages: each navigation call can override the defaults set in MauiProgram.
	internal static SharedTransitionOptions TransitionOptions { get; private set; } = SharedTransitionOptions.Default;

	void OnOptionsChanged(object? sender, EventArgs e)
	{
		if (PageAnimationPicker.SelectedIndex < 0 || PathPicker.SelectedIndex < 0)
			return;

		TransitionOptions = SharedTransitionOptions.Default.With(o =>
		{
			o.PageAnimation = PageAnimations[PageAnimationPicker.SelectedIndex].Value;
			o.Path = Paths[PathPicker.SelectedIndex].Value;
		});
	}

	async void OnFruitTapped(object? sender, TappedEventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not Fruit fruit)
			return;

		// The group selects which card of the list takes part in the transition.
		await this.PushWithSharedTransitionAsync(new DetailPage(fruit), group: fruit.Id, TransitionOptions);
	}
}
