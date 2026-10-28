# Plugin.Maui.SharedTransitions

Shared element ("hero") transitions between pages for .NET MAUI, inspired by
[Xamarin.Plugin.SharedTransitions](https://github.com/Evolutionlab/Xamarin.Plugin.SharedTransitions),
which was never ported to MAUI.

Works with **Shell** and **NavigationPage** because it doesn't replace MAUI's navigation:
it captures snapshots of the shared views, navigates without animation and animates the
snapshots in a native overlay to the matching views of the new page.

| Platform | Status |
|---|---|
| Android | ✅ |
| iOS / Mac Catalyst | ✅ (compiles, pending device testing) |
| Windows | Falls back to the default navigation animation |

## Setup

```csharp
builder
    .UseMauiApp<App>()
    .UseSharedTransitions(o => o.Duration = 250); // optional
```

## Usage

Mark the views with the same `Name` on both pages:

```xml
xmlns:st="clr-namespace:Plugin.Maui.SharedTransitions;assembly=Plugin.Maui.SharedTransitions"

<!-- List item -->
<Border st:SharedTransition.Name="card" st:SharedTransition.Group="{Binding Id}" ... />

<!-- Detail page -->
<Border st:SharedTransition.Name="card" ... />
```

Navigate with the extension methods:

```csharp
// From the list (group = the tapped item)
await this.PushWithSharedTransitionAsync(new DetailPage(item), group: item.Id);

// From the detail page
await this.PopWithSharedTransitionAsync();
```

To animate the back button too, call `PopWithSharedTransitionAsync` from
`Shell.BackButtonBehavior` and `OnBackButtonPressed` (see the sample).

## Animation styles

Choose how the rest of the page changes and the path of the shared elements, globally or per navigation:

```csharp
// Defaults for the whole app
builder.UseSharedTransitions(o =>
{
    o.PageAnimation = SharedTransitionPageAnimation.Slide;
    o.Path = SharedTransitionPath.Arc;
});

// Only for one navigation (copies the defaults and changes some values)
var options = SharedTransitionOptions.Default.With(o => o.PageAnimation = SharedTransitionPageAnimation.Scale);
await this.PushWithSharedTransitionAsync(new DetailPage(item), group: item.Id, options);
await this.PopWithSharedTransitionAsync(options);
```

| Option | Values | Default |
|---|---|---|
| `PageAnimation` | `Fade`: the previous page fades out over the new one<br>`Slide`: the new page slides in from the trailing edge (and back out on pop)<br>`Scale`: the pages zoom and crossfade, forward on push and backward on pop<br>`None`: only the shared elements are animated | `Fade` |
| `Path` | `Linear`: straight line<br>`Arc`: curved path that leaves horizontally, more natural for diagonal movements | `Linear` |
| `Duration` | Milliseconds | `250` |
| `Easing` | Any MAUI `Easing` | `Easing.CubicOut` |

With `Slide` and `Scale` the navigation bar of the new page appears right away; only the page content is animated.

## How it works

1. Snapshots of the shared views (children before parents) and of the window are placed in an overlay above everything.
2. The page is pushed/popped **without** animation.
3. When the destination is laid out, the matching views are measured and captured.
4. The snapshots move and scale to the new frames with a crossfade while the old page fades out.

A `Border` with a solid background and a rectangle/rounded rectangle shape is drawn natively instead,
so its corner radius, background, stroke color and thickness morph smoothly; its content flies as a snapshot on top.
A `Label` keeps the aspect ratio of its text while flying (scaled by its height and anchored like its
`HorizontalTextAlignment`), so the text doesn't get stretched when the label has a different width on each page.
Views without a match on the other page just fade out.

## Limitations (v0.1)

- Back gestures that MAUI handles by itself (iOS swipe-back) use the default animation.
- Only `Border` morphs its shape and colors; any other view (or a `Border` with a gradient) crossfades between both snapshots.
- Content loaded asynchronously in the destination (remote images) may appear at the end of the flight.

## Development

```
src/Plugin.Maui.SharedTransitions/      the library (NuGet package)
samples/SharedTransitions.Sample/       demo app (Shell, list → detail)
```

Requirements: .NET 10 SDK with the `maui`/`android`/`ios` workloads.

```bash
# Library, all target frameworks
dotnet build src/Plugin.Maui.SharedTransitions

# Run the sample on a connected Android device/emulator
dotnet build samples/SharedTransitions.Sample -f net10.0-android -t:Run

# NuGet package
dotnet pack src/Plugin.Maui.SharedTransitions -c Release
```

Platform code is selected by file suffix (see the `.csproj`): `*.android.cs`, `*.macios.cs`
(iOS + Mac Catalyst) and `*.fallback.cs` (Windows and plain `net10.0`).

## License

MIT
