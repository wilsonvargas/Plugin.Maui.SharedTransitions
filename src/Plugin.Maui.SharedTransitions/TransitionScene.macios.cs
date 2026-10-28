using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Platform;
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

	public partial ShapeLayer AddShape(Rect frame, ShapeStyle style)
	{
		// Full-size view, so the shape can be redrawn at any frame without relayout.
		var view = new UIView(_container.Bounds)
		{
			BackgroundColor = UIColor.Clear,
			UserInteractionEnabled = false,
			AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
		};
		var shape = new CAShapeLayer();
		view.Layer.AddSublayer(shape);
		_container.AddSubview(view);

		var layer = new ShapeLayer(view, shape);
		layer.Apply(frame, style, 1);
		return layer;
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

	public partial void BringToFront() => _view.Superview?.BringSubviewToFront(_view);

	// Snapshots are already bitmaps that Core Animation composites on the GPU.
	public partial void Cache()
	{
	}
}

internal sealed partial class ShapeLayer
{
	readonly UIView _view;
	readonly CAShapeLayer _shape;

	internal ShapeLayer(UIView view, CAShapeLayer shape)
	{
		_view = view;
		_shape = shape;
	}

	public partial void Apply(Rect frame, ShapeStyle style, double opacity)
	{
		// The stroke is centered on the outline, so it's inset by half its thickness to stay inside the frame.
		var inset = style.StrokeThickness / 2;
		var left = frame.X + inset;
		var top = frame.Y + inset;
		var right = frame.Right - inset;
		var bottom = frame.Bottom - inset;
		var maxRadius = Math.Max(0, Math.Min(right - left, bottom - top) / 2);

		nfloat Radius(double radius) => (nfloat)Math.Clamp(radius - inset, 0, maxRadius);
		var topLeft = Radius(style.CornerRadius.TopLeft);
		var topRight = Radius(style.CornerRadius.TopRight);
		var bottomRight = Radius(style.CornerRadius.BottomRight);
		var bottomLeft = Radius(style.CornerRadius.BottomLeft);

		var path = new CGPath();
		path.MoveToPoint((nfloat)left + topLeft, (nfloat)top);
		path.AddLineToPoint((nfloat)right - topRight, (nfloat)top);
		path.AddArcToPoint((nfloat)right, (nfloat)top, (nfloat)right, (nfloat)top + topRight, topRight);
		path.AddLineToPoint((nfloat)right, (nfloat)bottom - bottomRight);
		path.AddArcToPoint((nfloat)right, (nfloat)bottom, (nfloat)right - bottomRight, (nfloat)bottom, bottomRight);
		path.AddLineToPoint((nfloat)left + bottomLeft, (nfloat)bottom);
		path.AddArcToPoint((nfloat)left, (nfloat)bottom, (nfloat)left, (nfloat)bottom - bottomLeft, bottomLeft);
		path.AddLineToPoint((nfloat)left, (nfloat)top + topLeft);
		path.AddArcToPoint((nfloat)left, (nfloat)top, (nfloat)left + topLeft, (nfloat)top, topLeft);
		path.CloseSubpath();

		// Without this, Core Animation would animate every change implicitly and lag behind the transition.
		CATransaction.Begin();
		CATransaction.DisableActions = true;
		var previous = _shape.Path;
		_shape.Path = path;
		_shape.FillColor = style.Fill.ToCGColor();
		_shape.StrokeColor = (style.Stroke ?? Colors.Transparent).ToCGColor();
		_shape.LineWidth = (nfloat)style.StrokeThickness;
		CATransaction.Commit();
		previous?.Dispose();

		_view.Alpha = (nfloat)opacity;
	}

	public partial void BringToFront() => _view.Superview?.BringSubviewToFront(_view);
}
