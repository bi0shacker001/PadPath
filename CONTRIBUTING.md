# Contributing to PadPath

PadPath welcomes bug reports, accessibility feedback, documentation fixes, and focused pull requests.

## Development setup

1. Install the .NET 8 SDK on Windows 10 or 11.
2. Clone the repository.
3. Run `dotnet restore PadPath.sln`.
4. Run `dotnet build PadPath.sln -c Release`.
5. Run `dotnet format PadPath.sln --verify-no-changes` before submitting changes.

Use a controller when changing navigation behavior and verify keyboard-only operation. New visual states must preserve at least 4.5:1 text contrast and 3:1 non-text contrast.

By contributing, you agree that your contribution is licensed under GPL-3.0-only.
