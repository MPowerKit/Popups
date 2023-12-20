# MPowerKit.Popups

.NET MAUI popup library which allows you to open MAUI pages as a popup. Also the library allows you to use very simple and flexible animations for showing popup pages.

[![NuGet](https://img.shields.io/nuget/v/MPowerKit.Popups.svg?maxAge=2592000)](https://www.nuget.org/packages/MPowerKit.Popups)

Inspired by [Rg.Plugins.Popup](https://github.com/rotorgames/Rg.Plugins.Popup) and [Mopups](https://github.com/LuckyDucko/Mopups), but implementation is completely different. 

- It has almost the same PopupPage API as packages above, but improved animations, removed redundant properies as ```KeyboardOffset```, changed names of some properties. 

- Improved code and fixed some known bugs, eg Android window insets (system padding) or animation flickering. 

- Changed API of ```PopupService```, now you have an ability to choose a window to show/hide popup on.

- Under the hood platform specific code does not use custom renderers for ```PopupPage```.

- Hiding keyboard when tapping anywhere on popup except entry field

- ```PopupStack``` is not static from now.

- All API's are public or protected from now, so you can easily override and change implementation as you want

## Supported Platforms

* .NET8
* .NET8 for Android (min 7.0)
* .NET8 for iOS (min 13.0)
* .NET8 for MacCatalyst (min 13.1)
* .NET8 for Windows (min 10.0.17763.0)

Note: .NET8 for Tizen is not supported, but your PRs are welcome.

## Setup

Add ```UseMPowerKitPopups()``` to your MauiProgram.cs file as next

```csharp
builder
    .UseMauiApp<App>()
    .UseMPowerKitPopups();
```

## Usage

You can use both registered ```IPopupService``` or static singletone ```PopupService.Current```

Inherit your popup page from ```PopupPage```:

```csharp
public class YourCustomPopup : PopupPage...
```

Show popup:

```csharp
IPopupService _popupService;

YourCustomPopup _popup;

await _popupService.ShowPopupAsync(_popup, animated);
```

It has overload for showing popup which accepts a window of type ```Window```, on which the popup will be shown:

```csharp
IPopupService _popupService;

YourCustomPopup _popup;

await _popupService.ShowPopupAsync(_popup, desiredWindow, animated);
```

Hide popup (the last one from ```PopupStack```):

```csharp
IPopupService _popupService;

await _popupService.HidePopupAsync(animated);
```

And overload for hiding desired popup:

```csharp
IPopupService _popupService;

YourCustomPopup _popup;

await _popupService.HidePopupAsync(_popup, animated);
```

Note: Don't forget to catch informative exceptions;
