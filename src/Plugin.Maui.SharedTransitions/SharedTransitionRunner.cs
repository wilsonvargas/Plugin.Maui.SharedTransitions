using System.Diagnostics;

namespace Plugin.Maui.SharedTransitions;

internal static class SharedTransitionRunner
{
	const string AnimationName = "Plugin.Maui.SharedTransitions";

	// Slide: how far the page underneath moves, as a fraction of its width (parallax).
	const double SlideParallax = 0.3;

	// Scale: how much the pages zoom in or out.
	const double ScaleAmount = 0.1;

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

		// Text keeps its aspect ratio while flying, anchored like the label aligns it.
		public TextAlignment? TextAlignment { get; init; }

		public Rect? TargetFrame { get; set; }
		public SnapshotLayer? Target { get; set; }
		public ShapeStyle? TargetStyle { get; set; }
		public SnapshotLayer? TargetContent { get; set; }
	}

	// The rest of both pages, animated below the shared elements according to the page animation.
	sealed class PageLayers
	{
		public SnapshotLayer? Window { get; init; }
		public SnapshotLayer? Outgoing { get; init; }
		public SnapshotLayer? Incoming { get; set; }
		public PageBackground? Backdrop { get; set; }

		// Solid backgrounds that move with the page snapshots: on iOS a page view is transparent
		// (its background is drawn by the navigation container), so its snapshot is transparent too.
		public PageBackground? OutgoingBackground { get; set; }
		public PageBackground? IncomingBackground { get; set; }
	}

	sealed record PageBackground(ShapeLayer Layer, ShapeStyle Style);

	/// <summary>
	/// Captures the shared elements of <paramref name="from"/>, navigates without animation and
	/// animates the snapshots to the matching elements of <paramref name="to"/>.
	/// </summary>
	public static async Task RunAsync(Page from, Page to, object? group, bool isPop, Func<bool, Task> navigate, SharedTransitionOptions options)
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
			var pages = options.PageAnimation is SharedTransitionPageAnimation.Slide or SharedTransitionPageAnimation.Scale
				? new PageLayers { Outgoing = scene.Capture(from) }
				: new PageLayers { Window = scene.CaptureWindow() };

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

			if (pages.Outgoing is not null)
				AddIncomingPage(scene, from, to, isPop, options.PageAnimation, pages, flights);

			pages.Window?.Cache();
			pages.Outgoing?.Cache();
			pages.Incoming?.Cache();

			await AnimateAsync(to, options, t =>
			{
				StepPages(pages, isPop, options.PageAnimation, t);

				foreach (var flight in flights)
					Step(flight, options.Path, t);
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

		return scene.Capture(element) is SnapshotLayer layer
			? new Flight(name, layer.Frame) { Source = layer, TextAlignment = (element as Label)?.HorizontalTextAlignment }
			: null;
	}

	// Captures the destination page (shared elements already hidden) and stacks both pages below the flights.
	static void AddIncomingPage(TransitionScene scene, Page from, Page to, bool isPop, SharedTransitionPageAnimation animation, PageLayers pages, List<Flight> flights)
	{
		var outgoing = pages.Outgoing!;
		pages.Incoming = scene.Capture(to);
		pages.OutgoingBackground = AddBackground(scene, outgoing.Frame, from);
		pages.IncomingBackground = pages.Incoming is null ? null : AddBackground(scene, pages.Incoming.Frame, to);

		// While zooming, both pages are translucent: a backdrop with the page color hides the real page underneath.
		if (animation == SharedTransitionPageAnimation.Scale)
			pages.Backdrop = AddBackground(scene, outgoing.Frame, to);

		// The page that moves on top is the new one when pushing and the old one when popping.
		pages.Backdrop?.Layer.BringToFront();
		if (isPop)
		{
			pages.IncomingBackground?.Layer.BringToFront();
			pages.Incoming?.BringToFront();
			pages.OutgoingBackground.Layer.BringToFront();
			outgoing.BringToFront();
		}
		else
		{
			pages.OutgoingBackground.Layer.BringToFront();
			outgoing.BringToFront();
			pages.IncomingBackground?.Layer.BringToFront();
			pages.Incoming?.BringToFront();
		}

		foreach (var flight in flights)
		{
			flight.Shape?.BringToFront();
			flight.SourceContent?.BringToFront();
			flight.Source?.BringToFront();
		}

		foreach (var flight in flights)
		{
			flight.Target?.BringToFront();
			flight.TargetContent?.BringToFront();
		}
	}

	static PageBackground AddBackground(TransitionScene scene, Rect frame, Page page)
	{
		var style = new ShapeStyle(default, GetBackdropColor(page), null, 0);
		return new PageBackground(scene.AddShape(frame, style), style);
	}

	static Color GetBackdropColor(Page page)
	{
		if (((IView)page).Background is SolidPaint { Color: Color color } && color.Alpha > 0)
			return color;

		return Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.Black : Colors.White;
	}

	static void StepPages(PageLayers pages, bool isPop, SharedTransitionPageAnimation animation, double t)
	{
		pages.Window?.Apply(pages.Window.Frame, 1 - t);

		if (pages.Outgoing is not SnapshotLayer outgoing)
			return;

		var incoming = pages.Incoming;
		if (animation == SharedTransitionPageAnimation.Slide)
		{
			// Push: the new page covers the old one, which moves a bit to the leading edge. Pop: the reverse.
			if (isPop)
			{
				ApplyPage(outgoing, pages.OutgoingBackground, outgoing.Frame.Offset(outgoing.Frame.Width * t, 0), 1);
				if (incoming is not null)
					ApplyPage(incoming, pages.IncomingBackground, incoming.Frame.Offset(-incoming.Frame.Width * SlideParallax * (1 - t), 0), 1);
			}
			else
			{
				ApplyPage(outgoing, pages.OutgoingBackground, outgoing.Frame.Offset(-outgoing.Frame.Width * SlideParallax * t, 0), 1);
				if (incoming is not null)
					ApplyPage(incoming, pages.IncomingBackground, incoming.Frame.Offset(incoming.Frame.Width * (1 - t), 0), 1);
			}

			return;
		}

		// Scale: pushing zooms forward (the old page grows and fades, the new one grows into place);
		// popping zooms backward.
		var direction = isPop ? -1 : 1;
		ApplyPage(outgoing, pages.OutgoingBackground, ScaleAroundCenter(outgoing.Frame, 1 + direction * ScaleAmount * t), 1 - t);
		if (incoming is not null)
			ApplyPage(incoming, pages.IncomingBackground, ScaleAroundCenter(incoming.Frame, 1 - direction * ScaleAmount * (1 - t)), t);
	}

	static void ApplyPage(SnapshotLayer page, PageBackground? background, Rect frame, double opacity)
	{
		page.Apply(frame, opacity);
		background?.Layer.Apply(frame, background.Style, opacity);
	}

	static void Step(Flight flight, SharedTransitionPath path, double t)
	{
		if (flight.TargetFrame is not Rect targetFrame)
		{
			// No match on the destination: fade out in place.
			flight.Source?.Apply(flight.Frame, 1 - t);
			flight.Shape?.Apply(flight.Frame, flight.SourceStyle, 1 - t);
			flight.SourceContent?.Apply(flight.SourceContent.Frame, 1 - t);
			return;
		}

		var frame = Interpolate(flight.Frame, targetFrame, path, t);
		if (flight.Target is null)
		{
			flight.Source?.Apply(frame, 1);
		}
		else
		{
			// The new snapshot fades in during the first half and the old one fades out during the second,
			// so there is always one fully opaque and no trace of the old one is left at the end.
			flight.Source?.Apply(flight.TextAlignment is TextAlignment sourceAlignment ? FitText(flight.Frame, frame, sourceAlignment) : frame, Math.Min(1, 2 * (1 - t)));
			flight.Target.Apply(flight.TextAlignment is TextAlignment targetAlignment ? FitText(targetFrame, frame, targetAlignment) : frame, Math.Min(1, 2 * t));
		}

		if (flight.TargetStyle is ShapeStyle targetStyle)
			flight.Shape?.Apply(frame, ShapeStyle.Lerp(flight.SourceStyle, targetStyle, t), 1);
		else
			flight.Shape?.Apply(frame, flight.SourceStyle, 1 - t);

		// Contents morph between each other when both exist; otherwise they keep their place inside the shape.
		if (flight.SourceContent is not null && flight.TargetContent is not null)
		{
			var contentFrame = Interpolate(flight.SourceContent.Frame, flight.TargetContent.Frame, path, t);
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

	static Rect Interpolate(Rect from, Rect to, SharedTransitionPath path, double t)
	{
		var frame = Lerp(from, to, t);
		if (path != SharedTransitionPath.Arc)
			return frame;

		// Quadratic Bézier between both centers with the control point at (end X, start Y):
		// the element leaves horizontally and arrives vertically. Size still changes linearly.
		var start = from.Center;
		var end = to.Center;
		var u = 1 - t;
		var x = u * u * start.X + 2 * u * t * end.X + t * t * end.X;
		var y = u * u * start.Y + 2 * u * t * start.Y + t * t * end.Y;
		return new Rect(x - frame.Width / 2, y - frame.Height / 2, frame.Width, frame.Height);
	}

	// Labels usually have different widths on each page (a list cell vs. the full page), so stretching both
	// snapshots to the same frame deforms the text differently and the two copies never line up. Instead,
	// each snapshot keeps its aspect ratio, scaled by the height (which follows the font size), and is anchored
	// at the start, center or end of the interpolated frame like its text.
	static Rect FitText(Rect original, Rect frame, TextAlignment alignment)
	{
		var width = original.Width * frame.Height / original.Height;
		var x = alignment switch
		{
			TextAlignment.Center => frame.Center.X - width / 2,
			TextAlignment.End => frame.Right - width,
			_ => frame.X,
		};

		return new Rect(x, frame.Y, width, frame.Height);
	}

	static Rect ScaleAroundCenter(Rect rect, double scale)
	{
		var width = rect.Width * scale;
		var height = rect.Height * scale;
		return new Rect(rect.Center.X - width / 2, rect.Center.Y - height / 2, width, height);
	}

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
