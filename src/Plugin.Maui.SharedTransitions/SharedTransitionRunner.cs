using System.Diagnostics;

namespace Plugin.Maui.SharedTransitions;

internal static class SharedTransitionRunner
{
	const string AnimationName = "Plugin.Maui.SharedTransitions";

	static bool _isRunning;

	// A shared element on its way to the destination. Simple borders fly as a native shape that morphs
	// (corners, colors, stroke) with their content as a snapshot on top; anything else flies as a snapshot.
	sealed class Flight(string name, Rect frame)
	{
		public string Name { get; } = name;
		public Rect Frame { get; } = frame;
		public SnapshotLayer? Source { get; init; }
		public ShapeLayer? Shape { get; init; }
		public ShapeStyle SourceStyle { get; init; }
		public SnapshotLayer? SourceContent { get; set; }

		public Rect? TargetFrame { get; set; }
		public SnapshotLayer? Target { get; set; }
		public ShapeStyle? TargetStyle { get; set; }
		public SnapshotLayer? TargetContent { get; set; }
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
				if (CaptureSource(scene, name, element) is not Flight flight)
					continue;

				flights.Add(flight);
				Hide(element, hidden);
			}

			// Captured after hiding the elements, so they don't stay behind as a ghost.
			scene.HideScrollBars(from);
			var background = scene.CaptureWindow();

			await navigate(false);
			scene.HideScrollBars(to);
			await WaitForLayoutAsync(scene, to, group, options.LayoutTimeout);

			var targets = SharedElementFinder.Find(to, group).ToDictionary(t => t.Name, t => t.Element);
			foreach (var flight in flights)
			{
				if (!targets.TryGetValue(flight.Name, out var target) || scene.GetFrame(target) is not Rect frame)
					continue;

				flight.TargetFrame = frame;
				if (flight.Shape is not null && ShapeStyle.From(target) is ShapeStyle targetStyle)
				{
					flight.TargetStyle = targetStyle;
					flight.TargetContent = GetContent(target) is VisualElement content ? scene.Capture(content) : null;
				}
				else
				{
					flight.Target = scene.Capture(target);
				}

				Hide(target, hidden);
			}

			await AnimateAsync(to, options, t =>
			{
				background?.Apply(background.Frame, 1 - t);

				foreach (var flight in flights)
					Step(flight, t);
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

	static Flight? CaptureSource(TransitionScene scene, string name, VisualElement element)
	{
		if (ShapeStyle.From(element) is ShapeStyle style && scene.GetFrame(element) is Rect frame)
		{
			// The shape goes first so the content snapshot stays on top of it.
			return new Flight(name, frame)
			{
				Shape = scene.AddShape(frame, style),
				SourceStyle = style,
				SourceContent = GetContent(element) is VisualElement content ? scene.Capture(content) : null,
			};
		}

		return scene.Capture(element) is SnapshotLayer layer ? new Flight(name, layer.Frame) { Source = layer } : null;
	}

	static void Step(Flight flight, double t)
	{
		if (flight.TargetFrame is not Rect targetFrame)
		{
			// No match on the destination: fade out in place.
			flight.Source?.Apply(flight.Frame, 1 - t);
			flight.Shape?.Apply(flight.Frame, flight.SourceStyle, 1 - t);
			flight.SourceContent?.Apply(flight.SourceContent.Frame, 1 - t);
			return;
		}

		var frame = Lerp(flight.Frame, targetFrame, t);
		flight.Source?.Apply(frame, 1);
		flight.Target?.Apply(frame, t);

		if (flight.TargetStyle is ShapeStyle targetStyle)
			flight.Shape?.Apply(frame, ShapeStyle.Lerp(flight.SourceStyle, targetStyle, t), 1);
		else
			flight.Shape?.Apply(frame, flight.SourceStyle, 1 - t);

		// Contents morph between each other when both exist; otherwise they keep their place inside the shape.
		if (flight.SourceContent is not null && flight.TargetContent is not null)
		{
			var contentFrame = Lerp(flight.SourceContent.Frame, flight.TargetContent.Frame, t);
			flight.SourceContent.Apply(contentFrame, 1 - t);
			flight.TargetContent.Apply(contentFrame, t);
		}
		else
		{
			flight.SourceContent?.Apply(Map(flight.SourceContent.Frame, flight.Frame, frame), 1 - t);
			flight.TargetContent?.Apply(Map(flight.TargetContent.Frame, targetFrame, frame), t);
		}
	}

	static VisualElement? GetContent(VisualElement element) => (element as Border)?.Content as VisualElement;

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

	// Moves a rectangle that lives inside `from` to the same relative place inside `to`.
	static Rect Map(Rect rect, Rect from, Rect to)
	{
		var scaleX = to.Width / from.Width;
		var scaleY = to.Height / from.Height;
		return new Rect(
			to.X + (rect.X - from.X) * scaleX,
			to.Y + (rect.Y - from.Y) * scaleY,
			rect.Width * scaleX,
			rect.Height * scaleY);
	}
}
