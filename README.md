# iPhone Drive for Windows

Mount the Documents folders of iOS apps that enable Apple File Sharing as one Windows drive. The tray application detects a trusted device, chooses a free drive letter, discovers compatible apps, and reports the device's AFC storage capacity.

> Independent project: not affiliated with, sponsored by, or endorsed by Apple Inc. Apple, iPhone, iOS, and related marks are trademarks of Apple Inc.

## Features

- Combined folders such as `VLC`, `LocalSend`, and `a-Shell mini`
- Read, write, rename, and delete through Explorer
- Real AFC capacity instead of the Windows system-drive capacity
- Automatic device detection, pairing guidance, mount, and disconnect cleanup
- Free drive-letter selection from `I:` through `Z:`
- Original project artwork with no Apple logo

## Requirements

- 64-bit Windows 10 or Windows 11
- [Apple Devices for Windows](https://support.apple.com/guide/devices-windows/install-the-apple-devices-app-mchl5ded2763/windows), installed from Microsoft Store
- Dokan 2.3.1 or later (the release installer can install Dokan)
- An unlocked, trusted iPhone or iPad

Apple Mobile Device Support is deliberately **not redistributed** by this project. Install Apple Devices from Microsoft Store before installing iPhone Drive.

## Easy installation

1. Install **Apple Devices** from Microsoft Store by following [Apple's official Windows instructions](https://support.apple.com/guide/devices-windows/install-the-apple-devices-app-mchl5ded2763/windows).
2. Restart Windows after Apple Devices finishes installing.
3. Download `iPhoneDrive-Setup-1.0.0.exe` from this repository's **Releases** page.
4. Right-click the downloaded setup file, select **Properties**, and verify that the filename and publisher information are what you expect. The first release is not code-signed, so Windows SmartScreen may show an unknown-publisher warning.
5. Run the setup as administrator. It installs the required Dokan filesystem driver automatically.
6. Connect the iPhone with USB, unlock it, press **Trust** on the iPhone, and enter the iPhone passcode.
7. Start **iPhone Drive** from the Start menu. After a few seconds, open **This PC** and use the newly assigned drive letter.

The mounted drive contains the Documents folders of apps that enable iOS File Sharing, such as VLC and LocalSend. It does not expose protected iOS system files or every installed app.

### If the drive does not appear

- Keep the iPhone unlocked while it connects.
- Open Apple Devices once and confirm that the phone appears there.
- Disconnect and reconnect the USB cable, then use the tray icon's reconnect option.
- Restart Windows after installing Dokan or Apple Devices.
- Try a direct USB port and a known data-capable cable.

## Security and privacy

The application communicates locally with the connected device through libimobiledevice. It does not upload filenames, files, device identifiers, or diagnostics. See [PRIVACY.md](PRIVACY.md).

## Building

See [docs/BUILDING.md](docs/BUILDING.md). Release binaries must include the third-party license notices described in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## License

Original project code is MIT licensed. Third-party components keep their respective licenses. The aggregate proxy derives from the MIT-licensed Dokan.NET mirror sample. libimobiledevice and Dokan runtime components are LGPL licensed; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
