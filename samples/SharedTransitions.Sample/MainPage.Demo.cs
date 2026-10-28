using Plugin.Maui.SharedTransitions;

namespace SharedTransitions.Sample;

// Unattended demo used by the iOS simulator workflow, where there is no easy way to tap the screen:
// when the app is launched with SHARED_TRANSITIONS_DEMO=1 it pushes and pops a card with every page
// animation, slowly, and logs each step so the screenshots can be matched with the log.
public partial class MainPage
{
	const string DemoVariable = "SHARED_TRANSITIONS_DEMO";

	bool _demoStarted;

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (_demoStarted || Environment.GetEnvironmentVariable(DemoVariable) != "1")
			return;

		_demoStarted = true;
		RunDemo();
	}

	async void RunDemo()
	{
		try
		{
			var destination = Destination.All[1];
			await Task.Delay(3000);

			foreach (var animation in Enum.GetValues<SharedTransitionPageAnimation>())
			{
				TransitionOptions = SharedTransitionOptions.Default.With(o =>
				{
					o.PageAnimation = animation;
					o.Path = SharedTransitionPath.Arc;
					o.Duration = 2000;
				});

				var detail = new DetailPage(destination);
				Log($"push {animation} start");
				await this.PushWithSharedTransitionAsync(detail, destination.Id, TransitionOptions);
				Log($"push {animation} end");
				await Task.Delay(1500);

				Log($"pop {animation} start");
				await detail.PopWithSharedTransitionAsync(TransitionOptions);
				Log($"pop {animation} end");
				await Task.Delay(1500);
			}

			Log("done");
		}
		catch (Exception ex)
		{
			Log($"error {ex}");
		}
		finally
		{
			// Back to the options of the pickers, so tapping the app after the demo uses the normal speed.
			OnOptionsChanged(null, EventArgs.Empty);
		}
	}

	static void Log(string message) => Console.WriteLine($"DEMO {DateTime.UtcNow:HH:mm:ss.fff} {message}");
}
