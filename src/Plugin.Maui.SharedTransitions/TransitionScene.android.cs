using Android.Graphics;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Plugin.Maui.SharedTransitions;

internal sealed partial class TransitionScene
{
	readonly ViewGroup _decor;
	readonly FrameLayout _container;
	readonly List<Bitmap> _bitmaps = [];

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

		return Render(view) is Bitmap bitmap ? AddLayer(bitmap, frame, atBottom: false) : null;
	}

	public partial SnapshotLayer? CaptureWindow()
	{
		if (_decor.Width <= 0 || _decor.Height <= 0)
			return null;

		// Hide the overlay while drawing so the element snapshots don't end up in the background.
		_container.Visibility = ViewStates.Invisible;
		try
		{
			return Render(_decor) is Bitmap bitmap
				? AddLayer(bitmap, new Rect(0, 0, _decor.Width, _decor.Height), atBottom: true)
				: null;
		}
		finally
		{
			_container.Visibility = ViewStates.Visible;
		}
	}

	public partial void Dispose()
	{
		_decor.RemoveView(_container);
		foreach (var bitmap in _bitmaps)
			bitmap.Dispose();
		_bitmaps.Clear();
	}

	SnapshotLayer AddLayer(Bitmap bitmap, Rect frame, bool atBottom)
	{
		_bitmaps.Add(bitmap);

		var image = new ImageView(_container.Context!) { PivotX = 0, PivotY = 0 };
		image.SetScaleType(ImageView.ScaleType.FitXy!);
		image.SetImageBitmap(bitmap);

		var layoutParams = new FrameLayout.LayoutParams((int)frame.Width, (int)frame.Height);
		if (atBottom)
			_container.AddView(image, 0, layoutParams);
		else
			_container.AddView(image, layoutParams);

		var layer = new SnapshotLayer(image, frame);
		layer.Apply(frame, 1);
		return layer;
	}

	static Bitmap? Render(AView view)
	{
		if (view.Width <= 0 || view.Height <= 0)
			return null;

		try
		{
			// A Picture can record hardware bitmaps (Glide uses them), a software Canvas can't.
			if (OperatingSystem.IsAndroidVersionAtLeast(28))
			{
				using var picture = new Picture();
				var recording = picture.BeginRecording(view.Width, view.Height);
				view.Draw(recording);
				picture.EndRecording();
				return Bitmap.CreateBitmap(picture);
			}

			var bitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888!)!;
			using var canvas = new Canvas(bitmap);
			view.Draw(canvas);
			return bitmap;
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
}
