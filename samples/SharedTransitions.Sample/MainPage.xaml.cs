using Plugin.Maui.SharedTransitions;

namespace SharedTransitions.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		DestinationsView.ItemsSource = Destination.All;

		PageAnimationPicker.ItemsSource = Enum.GetNames<SharedTransitionPageAnimation>();
		PageAnimationPicker.SelectedIndex = (int)SharedTransitionOptions.Default.PageAnimation;
		PathPicker.ItemsSource = Enum.GetNames<SharedTransitionPath>();
		PathPicker.SelectedIndex = (int)SharedTransitionOptions.Default.Path;
	}

	// Options used by both pages: each navigation call can override the defaults set in MauiProgram.
	internal static SharedTransitionOptions TransitionOptions { get; private set; } = SharedTransitionOptions.Default;

	void OnOptionsChanged(object? sender, EventArgs e)
	{
		if (PageAnimationPicker.SelectedIndex < 0 || PathPicker.SelectedIndex < 0)
			return;

		TransitionOptions = SharedTransitionOptions.Default.With(o =>
		{
			o.PageAnimation = (SharedTransitionPageAnimation)PageAnimationPicker.SelectedIndex;
			o.Path = (SharedTransitionPath)PathPicker.SelectedIndex;
		});
	}

	async void OnDestinationTapped(object? sender, TappedEventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not Destination destination)
			return;

		// The group selects which card of the list takes part in the transition.
		await this.PushWithSharedTransitionAsync(new DetailPage(destination), group: destination.Id, TransitionOptions);
	}
}
