# DataOrganiser <img src="icon.png" width="48" align="right" />

A file finder and data organiser tool in one, built to be powerful even with limited information.

Scan a folder or an entire drive, then narrow down what you're looking for by extension, partial name, and date in any combination.

Examples:
- You know it's a PDF, roughly from last month, and the partial name has "invoice" somewhere in it.
- You are looking to compile all of your music files, using the Audio Files extension filter or a custom Music extension filter (configurable in settings) to list all of your music.
- You made a folder for a project a few weeks ago and can't remember where you put it.
- You just saved something and it went into the wrong folder - Recent Dump shows everything created in a recent time window (configurable in settings).

It also has basic file management built in with features to move, copy, delete, open, or open to the file's location in Explorer.

## Getting Started

### Build from source (recommended)

Requirements: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

```bash
git clone https://github.com/luma162/DataOrganiser
cd DataOrganiser
dotnet publish -c Release
```

The finished executable will be in `bin\Release\net9.0-windows\publish\win-x64\DataOrganiser.exe`.

### Prebuilt release

Alternatively, grab the `.exe` from the [Releases page](https://github.com/luma162/DataOrganiser/releases) - it is unsigned and may cause a SmartScreen warning.

## Screenshots

<img src="screenshots/1.png" width="800"> <br>
<img src="screenshots/2.png" width="600"> <br>
<img src="screenshots/3.png" width="600">

## License

Licensed under the [MIT License](LICENSE) — free to use, modify, and distribute, including commercially, with attribution.