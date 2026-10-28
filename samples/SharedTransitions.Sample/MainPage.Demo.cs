using Plugin.Maui.SharedTransitions;

namespace SharedTransitions.Sample;

// Unattended demo used to test on a simulator, where there is no easy way to tap the screen, and to record
// the GIF of the README: when the app is launched with SHARED_TRANSITIONS_DEMO=1 it pushes and pops a different
// fruit with every page animation (the pickers show the current one) and logs each step, so screenshots can be
// matched with the log. SHARED_TRANSITIONS_DEMO_DURATION sets the duration in milliseconds (default: 2000).
public partial class MainPage
{
	const string DemoVariable = "SHARED_TRANSITIONS_DEMO";
	const string DemoDurationVariable = "SHARED_TRANSITIONS_DEMO_DURATION";

	static readonly (int Fruit, SharedTransitionPageAnimation Animation, SharedTransitionPath Path)[] DemoSteps =
	[
		(0, SharedTransitionPageAnimation.Fade, SharedTransitionPath.Linear),
		(3, SharedTransitionPageAnimation.Slide, SharedTransitionPath.Arc),
		(4, SharedTransitionPageAnimation.Scale, SharedTransitionPath.Arc),
	];

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
			var duration = uint.TryParse(Environment.GetEnvironmentVariable(DemoDurationVariable), out var value) ? value : 2000u;
			await Task.Delay(3000);

			foreach (var (index, animation, path) in DemoSteps)
			{
				// The pickers update TransitionOptions, so the recording shows which animation is running.
				PageAnimationPicker.SelectedIndex = Array.FindIndex(PageAnimations, o => o.Value == animation);
				PathPicker.SelectedIndex = Array.FindIndex(Paths, o => o.Value == path);
				TransitionOptions = TransitionOptions.With(o => o.Duration = duration);
				await Task.Delay(1000);

				var fruit = Fruit.All[index];
				var detail = new DetailPage(fruit);
				Log($"push {animation} start");
				await this.PushWithSharedTransitionAsync(detail, fruit.Id, TransitionOptions);
				Log($"push {animation} end");
				await Task.Delay(1500);

				Log($"pop {animation} start");
				await detail.PopWithSharedTransitionAsync(TransitionOptions);
				Log($"pop {animation} end");
				await Task.Delay(1000);
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
