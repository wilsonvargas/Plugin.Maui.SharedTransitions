using CoreGraphics;
using UIKit;

namespace Plugin.Maui.SharedTransitions;

internal sealed partial class TransitionScene
{
	readonly UIWindow _window;
	readonly UIView _container;

	TransitionScene(UIWindow window)
	{
		_window = window;
		// UserInteractionEnabled so touches don't reach the pages while the transition runs.
		_container = new UIView(window.Bounds)
		{
			BackgroundColor = UIColor.Clear,
			UserInteractionEnabled = true,
			AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
		};
		window.AddSubview(_container);
	}

	public static partial TransitionScene? TryCreate(VisualElement anchor)
	{
		var window = GetNativeView(anchor)?.Window
			?? Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController()?.View?.Window;

		return window is null ? null : new TransitionScene(window);
	}

	public partial Rect? GetFrame(VisualElement element)
	{
		var view = GetNativeView(element);
		if (view?.Window is null || view.Bounds.Width <= 0 || view.Bounds.Height <= 0)
			return null;

		var frame = view.ConvertRectToView(view.Bounds, _container);
		return new Rect(frame.X, frame.Y, frame.Width, frame.Height);
	}

	public partial SnapshotLayer? Capture(VisualElement element)
	{
		if (GetNativeView(element) is not UIView view || GetFrame(element) is not Rect frame)
			return null;

		return AddLayer(Render(view), frame, atBottom: false);
	}

	public partial SnapshotLayer? CaptureWindow()
	{
		// The root view is a sibling of the overlay, so the element snapshots are not included.
		if (_window.RootViewController?.View is not UIView root || root.Bounds.Width <= 0)
			return null;

		var frame = root.ConvertRectToView(root.Bounds, _container);
		return AddLayer(Render(root), new Rect(frame.X, frame.Y, frame.Width, frame.Height), atBottom: true);
	}

	public partial void Dispose()
	{
		foreach (var subview in _container.Subviews)
		{
			if (subview is UIImageView imageView)
				imageView.Image?.Dispose();
		}

		_container.RemoveFromSuperview();
		_container.Dispose();
	}

	SnapshotLayer AddLayer(UIImage image, Rect frame, bool atBottom)
	{
		var imageView = new UIImageView(image) { ContentMode = UIViewContentMode.ScaleToFill };
		if (atBottom)
			_container.InsertSubview(imageView, 0);
		else
			_container.AddSubview(imageView);

		var layer = new SnapshotLayer(imageView, frame);
		layer.Apply(frame, 1);
		return layer;
	}

	static UIImage Render(UIView view)
	{
		using var renderer = new UIGraphicsImageRenderer(view.Bounds.Size);
		return renderer.CreateImage(_ => view.DrawViewHierarchy(view.Bounds, true));
	}

	static UIView? GetNativeView(VisualElement element) =>
		element.Handler is IViewHandler handler ? (handler.ContainerView ?? handler.PlatformView) as UIView : null;
}

internal sealed partial class SnapshotLayer
{
	readonly UIImageView _view;

	internal SnapshotLayer(UIImageView view, Rect frame)
	{
		_view = view;
		Frame = frame;
	}

	public partial void Apply(Rect frame, double opacity)
	{
		_view.Frame = new CGRect(frame.X, frame.Y, frame.Width, frame.Height);
		_view.Alpha = (nfloat)opacity;
	}
}
