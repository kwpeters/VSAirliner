# VSAirliner

A Visual Studio extension containing some handy commands.

## Installing

To install this extension:

1. Build the release version.
2. If an older version of this extension is already installed, uninstall it.
3. Close all instances of Visual Studio.
4. Run the installer: `VSAirliner\bin\Release\VSAirliner.vsix`

## Adding a New Command

- In the Solution Explorer, right click on the project and choose Add > New Item...
- In the Search box, search for "Command"
- Choose the "Command" C# Item and give it a name starting with "VSAirliner..."

## Debugging

This project is configured to launch Visual Studio when debugging.

Debugging a new version only seems to work if the extension is not currently
installed.

If you notice that changes are not showing up when debugging this extension,
try resetting the experimental instance.  To do this, run the following shortcut:

Start menu > Visual Studio 2022 > Reset the Visual Studio 2022 Experimental Instance LTSC 17.2
