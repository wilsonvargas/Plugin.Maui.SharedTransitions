using System.Diagnostics;

namespace Plugin.Maui.SharedTransitions;

internal static class SharedTransitionRunner
{
	const string AnimationName = "Plugin.Maui.SharedTransitions";

	static bool _isRunning;

	sealed class Flight(string name, SnapshotLayer source)
	{
		public string Name { get; } = name;
		public SnapshotLayer Source { get; } = source;
		public SnapshotLayer? Target { get; set; }
		public Rect? TargetFrame { get; set; }
	}

	/// <summary>
	/// Captures the shared elements of <paramref name="from"/>, navigates without animation and
	/// animates the snapshots to the matching elements of <paramref name="to"/>.
	/// </summary>
	public static async Task RunAsync(Page from, Page to, object? group, Func<bool, Task> navigate, SharedTransitionOptions options)
	{
		var sources = SharedElementFinder.Find(from, group);
		var scene = _isRunning || sources.Count == 0 ? null : TransitionScene.TryCreate(from);
		if (scene is null)
		{
			await navigate(true);
			return;
		}

		_isRunning = true;
		var hidden = new List<(VisualElement Element, double Opacity)>();
		try
		{
			// Children come before parents, so a nested element is hidden before its parent is captured.
			var flights = new List<Flight>();
			foreach (var (name, element) in sources)
			{
				if (scene.Capture(element) is not SnapshotLayer layer)
					continue;

				flights.Add(new Flight(name, layer));
				Hide(element, hidden);
			}

			// Captured after hiding the elements, so they don't stay behind as a ghost.
			var background = scene.CaptureWindow();

			await navigate(false);
			await WaitForLayoutAsync(scene, to, group, options.LayoutTimeout);

			var targets = SharedElementFinder.Find(to, group).ToDictionary(t => t.Name, t => t.Element);
			foreach (var flight in flights)
			{
				if (!targets.TryGetValue(flight.Name, out var target) || scene.GetFrame(target) is not Rect frame)
					continue;

				flight.TargetFrame = frame;
				flight.Target = scene.Capture(target);
				flight.Target?.Apply(flight.Source.Frame, 0);
				Hide(target, hidden);
			}

			await AnimateAsync(to, options, t =>
			{
				background?.Apply(background.Frame, 1 - t);

				foreach (var flight in flights)
				{
					if (flight.TargetFrame is not Rect targetFrame)
					{
						// No match on the destination: fade out in place.
						flight.Source.Apply(flight.Source.Frame, 1 - t);
						continue;
					}

					var frame = Lerp(flight.Source.Frame, targetFrame, t);
					flight.Source.Apply(frame, 1);
					flight.Target?.Apply(frame, t);
				}
			});
		}
		finally
		{
			foreach (var (element, opacity) in hidden)
				element.Opacity = opacity;

			scene.Dispose();
			_isRunning = false;
		}
	}

	static void Hide(VisualElement element, List<(VisualElement, double)> hidden)
	{
		hidden.Add((element, element.Opacity));
		element.Opacity = 0;
	}

	// Waits until every shared element of the page has a native frame that doesn't change between two frames.
	static async Task WaitForLayoutAsync(TransitionScene scene, Page page, object? group, TimeSpan timeout)
	{
		var stopwatch = Stopwatch.StartNew();
		List<Rect?>? previous = null;

		while (stopwatch.Elapsed < timeout)
		{
			await Task.Delay(16);

			var frames = SharedElementFinder.Find(page, group).Select(e => scene.GetFrame(e.Element)).ToList();
			if (frames.Count > 0 && frames.All(f => f is not null) && previous is not null && frames.SequenceEqual(previous))
				return;

			previous = frames;
		}
	}

	static Task AnimateAsync(VisualElement owner, SharedTransitionOptions options, Action<double> step)
	{
		var completion = new TaskCompletionSource();
		step(0);

		new Animation(step, 0, 1, options.Easing).Commit(
			owner,
			AnimationName,
			rate: 16,
			length: options.Duration,
			finished: (_, _) =>
			{
				step(1);
				completion.TrySetResult();
			});

		return completion.Task;
	}

	static Rect Lerp(Rect from, Rect to, double t) => new(
		from.X + (to.X - from.X) * t,
		from.Y + (to.Y - from.Y) * t,
		from.Width + (to.Width - from.Width) * t,
		from.Height + (to.Height - from.Height) * t);
}
