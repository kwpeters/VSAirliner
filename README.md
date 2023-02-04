# VSAirliner

A Visual Studio extension containing some handy commands.

## Installing

To install this extension:

1. Build the release version.
2. If an older version of this extension is already installed, uninstall it.
3. Close all instances of Visual Studio.
4. Run the installer: `VSAirliner\bin\Release\VSAirliner.vsix`

## Debugging

This project is configured to launch Visual Studio when debugging.  In that
instance, I have been assigning the current command to the `Ctrl+Num 0` keyboard
shortcut.

If you notice that changes are not showing up when debugging this extension,
try resetting the experimental instance.  To do this, run the following shortcut:

Start menu > Visual Studio 2022 > Reset the Visual Studio 2022 Experimental Instance LTSC 17.2
