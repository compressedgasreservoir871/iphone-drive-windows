# Building on Windows

Prerequisites: .NET 8 SDK, .NET Framework 4.x C# compiler, Inno Setup 6, Dokan 2.3.1 SDK/runtime, and compatible libimobiledevice Windows binaries.

Run the checked-in build script from PowerShell:

```powershell
.\scripts\build.ps1 `
  -LibIMobileRuntime 'C:\path\to\libimobiledevice-runtime' `
  -DokanMsi 'C:\path\to\Dokan_x64.msi' `
  -DokanDll 'C:\Program Files\Dokan\Dokan Library-2.3.1\dokan2.dll' `
  -BuildInstaller
```

`-DokanMsi` must point to the x64 MSI expected by the installer. Inputs are intentionally not downloaded automatically: release maintainers must obtain them from official upstream releases, review their licenses, and verify their checksums.

1. Publish `src/aggregate-proxy/AggregateProxy.csproj` as a self-contained `win-x64` executable.
2. Compile `src/ifuse-win/ifuse-win.cs` and `src/tray/iphone-drive-tray.cs` for x64, embedding `assets/iphone-drive.ico`.
3. Place runtime DLLs and applicable third-party license texts in the installer payload.
4. Obtain Dokan only from its official release and verify its checksum.
5. Do not add or package Apple Mobile Device Support. The installer must require Apple Devices from Microsoft Store.
6. Compile the Inno Setup definition under `installer/`.

Never commit generated binaries, pairing records, Apple installers, signing keys, or local mount data.
