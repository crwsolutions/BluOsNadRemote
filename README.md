# OPEN BLUOS NAD REMOTE
Opensource and nicely featured remote control for Bluesound products. 

Like the official BluOS Controller this app serves as a universal remote for all BluOS-enabled wireless hi-res music systems, including Bluesound, NAD Electronics, PSB Speakers, Bluesound Professional, DALI Speakers, Monitor Audio and Cyrus Audio.

Features on top of that an advanced tab to send specific NAD Electronics Txxx ethernet (RS232) commands.

# How to contribute
Make sure you have your Visual Studio and .NET 8 environment set up for .NET MAUI development.

This app is build very straight forward. So it should not be to hard to contribute. However it's heavily dependent on the following packages:
- [Blu4Net](https://github.com/roblans/Blu4Net), managing the layer between the app and the players
- CommunityToolkit.Mvvm, that helps greatly reduce boilerplate.
- [SourceDepend](https://github.com/crwsolutions/sourcedepend), that helps greatly reduce dependency injection (DI) boilerplate[^1].

[^1] This is a somewhat experimental package. You will encounter this [Dependency] attributes all over the place, these fields will be injected via a generated constructor.
```csharp
[Dependency]
private readonly AnotherService anotherService;
```

I would kindly ask you to continue applying the features of these packages in your contributions.

# My test setup
This app has been thoroughly tested with the following things:
- NAD T758 V3
- BLUESOUND PULSE M (P230)
- Android
- Tidal
- TuneIn
- Local library

And less with:
- iPhone 12

And not so much with:
- Windows 10/ 11

And absolutely not with all other things you can't possibly imagine but still are missing like Spotify. But should just work 🤞.