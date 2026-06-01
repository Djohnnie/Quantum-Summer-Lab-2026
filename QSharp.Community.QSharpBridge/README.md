# QSharp.Community.QSharpBridge

NuGet package exposing the native functionalities of the `qsharp-bridge`.

## Building

0. ...

    `cargo install uniffi-bindgen-cs --git https://github.com/NordSecurity/uniffi-bindgen-cs --tag v0.10.0+v0.29.4`

1. Make sure the Rust project is built in release mode

    `cargo build --release`

2. Build the .NET library

    `dotnet build -c release`

The NuGet package will be located under `./bin/release/QSharp.Community.QSharpBridge.{version}.nupkg`