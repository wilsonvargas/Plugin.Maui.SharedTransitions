using Plugin.Maui.SharedTransitions;

namespace SharedTransitions.Sample;

public partial class DetailPage : ContentPage
{
	public DetailPage(Destination destination)
	{
		InitializeComponent();
		BindingContext = destination;

		// The Shell back arrow also goes back with the shared transition.
		BackBehavior.Command = new Command(GoBack);
	}

	void OnBackClicked(object? sender, EventArgs e) => GoBack();

	// Android hardware/gesture back.
	protected override bool OnBackButtonPressed()
	{
		GoBack();
		return true;
	}

	async void GoBack() => await this.PopWithSharedTransitionAsync();
}
