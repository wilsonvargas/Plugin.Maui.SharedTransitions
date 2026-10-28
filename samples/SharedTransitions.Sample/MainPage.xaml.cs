using Plugin.Maui.SharedTransitions;

namespace SharedTransitions.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		DestinationsView.ItemsSource = Destination.All;
	}

	async void OnDestinationTapped(object? sender, TappedEventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not Destination destination)
			return;

		// The group selects which card of the list takes part in the transition.
		await this.PushWithSharedTransitionAsync(new DetailPage(destination), group: destination.Id);
	}
}
