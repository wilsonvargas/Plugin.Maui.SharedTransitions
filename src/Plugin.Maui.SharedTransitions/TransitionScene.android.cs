using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using Paint = Android.Graphics.Paint;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Plugin.Maui.SharedTransitions;

internal sealed partial class TransitionScene
{
	readonly ViewGroup _decor;
	readonly FrameLayout _container;
	readonly List<IDisposable> _snapshots = [];
	readonly List<(AView View, bool Vertical, bool Horizontal)> _scrollBars = [];

	TransitionScene(ViewGroup decor)
	{
		_decor = decor;
		// Clickable so taps don't reach the pages while the transition runs.
		_container = new FrameLayout(decor.Context!) { Clickable = true };
		decor.AddView(_container, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
	}

	public static partial TransitionScene? TryCreate(VisualElement anchor)
	{
		var activity = anchor.Handler?.MauiContext?.Context?.GetActivity()
			?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

		return activity?.Window?.DecorView is ViewGroup decor ? new TransitionScene(decor) : null;
	}

	public partial Rect? GetFrame(VisualElement element)
	{
		var view = GetNativeView(element);
		if (view is null || !view.IsAttachedToWindow || view.Width <= 0 || view.Height <= 0)
			return null;

		var location = new int[2];
		var origin = new int[2];
		view.GetLocationInWindow(location);
		_decor.GetLocationInWindow(origin);

		return new Rect(location[0] - origin[0], location[1] - origin[1], view.Width, view.Height);
	}

	public partial SnapshotLayer? Capture(VisualElement element)
	{
		if (GetNativeView(element) is not AView view || GetFrame(element) is not Rect frame)
			return null;

		return Render(view) is Drawable snapshot ? AddLayer(snapshot, frame, atBottom: false) : null;
	}

	public partial ShapeLayer AddShape(Rect frame, ShapeStyle style)
	{
		var view = new ShapeView(_container.Context!);
		_container.AddView(view, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

		var layer = new ShapeLayer(view);
		layer.Apply(frame, style, 1);
		return layer;
	}

	public partial SnapshotLayer? CaptureWindow()
	{
		if (_decor.Width <= 0 || _decor.Height <= 0)
			return null;

		// Hide the overlay while drawing so the element snapshots don't end up in the background.
		_container.Visibility = ViewStates.Invisible;
		try
		{
			return Render(_decor) is Drawable snapshot
				? AddLayer(snapshot, new Rect(0, 0, _decor.Width, _decor.Height), atBottom: true)
				: null;
		}
		finally
		{
			_container.Visibility = ViewStates.Visible;
		}
	}

	partial void HideScrollBarsCore(VisualElement page)
	{
		if (GetNativeView(page) is AView view)
			HideScrollBars(view);
	}

	void HideScrollBars(AView view)
	{
		if (view.VerticalScrollBarEnabled || view.HorizontalScrollBarEnabled)
		{
			_scrollBars.Add((view, view.VerticalScrollBarEnabled, view.HorizontalScrollBarEnabled));
			view.VerticalScrollBarEnabled = false;
			view.HorizontalScrollBarEnabled = false;
		}

		if (view is ViewGroup group)
		{
			for (var i = 0; i < group.ChildCount; i++)
			{
				if (group.GetChildAt(i) is AView child)
					HideScrollBars(child);
			}
		}
	}

	public partial void Dispose()
	{
		// Enabling them again doesn't wake them up, so they stay hidden until the user scrolls.
		foreach (var (view, vertical, horizontal) in _scrollBars)
		{
			view.VerticalScrollBarEnabled = vertical;
			view.HorizontalScrollBarEnabled = horizontal;
		}
		_scrollBars.Clear();

		_decor.RemoveView(_container);
		foreach (var snapshot in _snapshots)
			snapshot.Dispose();
		_snapshots.Clear();
	}

	SnapshotLayer AddLayer(Drawable snapshot, Rect frame, bool atBottom)
	{
		_snapshots.Add(snapshot);

		var image = new ImageView(_container.Context!) { PivotX = 0, PivotY = 0 };
		image.SetScaleType(ImageView.ScaleType.FitXy!);
		image.SetImageDrawable(snapshot);

		var layoutParams = new FrameLayout.LayoutParams((int)frame.Width, (int)frame.Height);
		if (atBottom)
			_container.AddView(image, 0, layoutParams);
		else
			_container.AddView(image, layoutParams);

		var layer = new SnapshotLayer(image, frame);
		layer.Apply(frame, 1);
		return layer;
	}

	Drawable? Render(AView view)
	{
		if (view.Width <= 0 || view.Height <= 0)
			return null;

		try
		{
			// Recording a Picture is cheap and it is replayed by the GPU; rasterizing it into a Bitmap
			// took tens of milliseconds per element. It also records hardware bitmaps (Glide uses them).
			if (OperatingSystem.IsAndroidVersionAtLeast(28))
			{
				var picture = new Picture();
				var recording = picture.BeginRecording(view.Width, view.Height);
				view.Draw(recording);
				picture.EndRecording();
				return new PictureDrawable(picture);
			}

			var bitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888!)!;
			using (var canvas = new Canvas(bitmap))
				view.Draw(canvas);
			return new BitmapDrawable(_container.Resources, bitmap);
		}
		catch (Java.Lang.IllegalArgumentException)
		{
			// Hardware bitmaps on API 26-27: skip this element instead of crashing.
			return null;
		}
	}

	static AView? GetNativeView(VisualElement element) =>
		element.Handler is IViewHandler handler ? (handler.ContainerView ?? handler.PlatformView) as AView : null;
}

internal sealed partial class SnapshotLayer
{
	readonly ImageView _view;

	internal SnapshotLayer(ImageView view, Rect frame)
	{
		_view = view;
		Frame = frame;
	}

	public partial void Apply(Rect frame, double opacity)
	{
		_view.TranslationX = (float)frame.X;
		_view.TranslationY = (float)frame.Y;
		_view.ScaleX = (float)(frame.Width / Frame.Width);
		_view.ScaleY = (float)(frame.Height / Frame.Height);
		_view.Alpha = (float)opacity;
	}

	public partial void BringToFront() => _view.BringToFront();

	public partial void Clip(Rect rect) =>
		_view.ClipBounds = new Android.Graphics.Rect((int)rect.X, (int)rect.Y, (int)Math.Ceiling(rect.Right), (int)Math.Ceiling(rect.Bottom));

	// Without it, a translucent full-page Picture is replayed into an offscreen buffer on every frame.
	public partial void Cache() => _view.SetLayerType(LayerType.Hardware, null);
}

internal sealed partial class ShapeLayer
{
	readonly ShapeView _view;

	internal ShapeLayer(ShapeView view) => _view = view;

	public partial void Apply(Rect frame, ShapeStyle style, double opacity)
	{
		_view.Update(frame, style);
		_view.Alpha = (float)opacity;
	}

	public partial void BringToFront() => _view.BringToFront();
}

// Full-size view that draws a rounded rectangle at any frame, so resizing it never triggers a layout pass.
internal sealed class ShapeView : AView
{
	readonly Paint _fill = new(PaintFlags.AntiAlias);
	readonly Paint _stroke = new(PaintFlags.AntiAlias);
	readonly Android.Graphics.Path _path = new();
	readonly float[] _radii = new float[8];
	readonly float _density;

	public ShapeView(Context context) : base(context)
	{
		_stroke.SetStyle(Paint.Style.Stroke);
		_density = context.Resources?.DisplayMetrics?.Density ?? 1;
	}

	public void Update(Rect frame, ShapeStyle style)
	{
		// The stroke is centered on the outline, so it's inset by half its thickness to stay inside the frame.
		var strokeWidth = (float)(style.StrokeThickness * _density);
		var inset = strokeWidth / 2;
		var left = (float)frame.X + inset;
		var top = (float)frame.Y + inset;
		var right = (float)frame.Right - inset;
		var bottom = (float)frame.Bottom - inset;
		var maxRadius = Math.Max(0, Math.Min(right - left, bottom - top) / 2);

		// Path radii go clockwise from the top-left corner: TL, TR, BR, BL (x and y for each one).
		SetRadius(0, style.CornerRadius.TopLeft, inset, maxRadius);
		SetRadius(1, style.CornerRadius.TopRight, inset, maxRadius);
		SetRadius(2, style.CornerRadius.BottomRight, inset, maxRadius);
		SetRadius(3, style.CornerRadius.BottomLeft, inset, maxRadius);

		_path.Reset();
		_path.AddRoundRect(left, top, right, bottom, _radii, Android.Graphics.Path.Direction.Cw!);

		_fill.Color = style.Fill.ToPlatform();
		_stroke.Color = (style.Stroke ?? Colors.Transparent).ToPlatform();
		_stroke.StrokeWidth = strokeWidth;

		Invalidate();
	}

	void SetRadius(int corner, double radius, float inset, float maxRadius)
	{
		var value = Math.Clamp((float)(radius * _density) - inset, 0, maxRadius);
		_radii[corner * 2] = value;
		_radii[corner * 2 + 1] = value;
	}

	// A single path: fading it doesn't need an offscreen buffer, which matters for full-page backgrounds.
	public override bool HasOverlappingRendering => false;

	protected override void OnDraw(Canvas canvas)
	{
		canvas.DrawPath(_path, _fill);
		if (_stroke.StrokeWidth > 0)
			canvas.DrawPath(_path, _stroke);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_fill.Dispose();
			_stroke.Dispose();
			_path.Dispose();
		}

		base.Dispose(disposing);
	}
}
