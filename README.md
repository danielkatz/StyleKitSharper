# StyleKitSharper
[StyleKitSharper](http://stylekitsharper.azurewebsites.net/) is a tool that transpiles StyleKits produced by [PaintCode 3](https://www.paintcodeapp.com/) from Java to C# for use in Xamarin.Android projects.

Since PaintCode 3 doesn't provide a built-in code generator for Android C# Xamarin, StyleKitSharper aims to close this gap until the official support will be ready.

## Usage
You can transpile your StyleKits by using the [live](http://stylekitsharper.azurewebsites.net/) version.
Alternatively, you can use the command-line tool:
```
sks MyStyleKit.java MyStyleKit.cs -n MyApp.Android
```

## Contribute
If you having an issue with StyleKitSharper's output, please open an issue and try to provide a useful description of your problem. If you know how to improve something, contributions are very welcome!

For sugesstions contact me via me@danielkatz.net.

## Disclaimer
**This in NOT a general purpose transpiler! StyleKitSharper can transpile only StyleKits produced by PaintCode 3.**
