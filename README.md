# SteelseriesSonar — SteelSeries GG Sonar Macro Deck 3 Plugin

[![Build & Package](https://github.com/Cjhackeryt/SteelseriesSonar/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/Cjhackeryt/SteelseriesSonar/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Target: .NET 10](https://img.shields.io/badge/.NET-10.0-512bd4.svg)](https://dotnet.microsoft.com/)
[![Macro Deck](https://img.shields.io/badge/Macro%20Deck-3.0+-0088cc.svg)](https://macro-deck.app/)

A professional, high-performance **Macro Deck 3** plugin for controlling **SteelSeries GG Sonar** audio directly from your stream deck, tablet, or secondary monitor.

Control channel volumes, toggle mute states, adjust Chat Mix balance, and switch between Streamer Mode mixes (Streaming vs. Monitoring) with zero perceptible latency.

---

## ✨ Features

- 🔊 **Precise Volume Control**: Set exact volume levels (`0–100%`) or adjust up/down incrementally with customizable step sizes.
- 🔇 **Mute & Toggle**: Explicitly mute/unmute or use single-button toggle actions with live feedback.
- 🎚️ **Chat Mix Control**: Seamlessly adjust the balance slider between game and voice communication (`-100` full chat to `+100` full game).
- 🎙️ **Streamer Mode Support**: Full dual-slider support for Streamer Mode — independently control what your stream hears (**Streaming**) and what you hear in your headset (**Monitoring**).
- 🔄 **Automatic Dynamic Discovery**: Automatically detects Sonar's local dynamic web server port on startup and handles GG restarts gracefully without manual configuration.
- ⚡ **Optimized Architecture**: Powered by .NET 10 and `IHttpClientFactory` connection pooling for instant response times and low resource usage.

---

## 🎧 Supported Audio Channels

| Channel ID | Name in Plugin | Description |
|---|---|---|
| `master` | **Master** | Master output mix for all Sonar audio |
| `game` | **Game** | Game audio channel |
| `chatRender` | **Chat (Playback)** | Voice chat audio output (Discord, Teams, in-game chat) |
| `chatCapture` | **Microphone (Capture)** | Your microphone / input audio sent to voice chat/stream |
| `media` | **Media** | Music and browser media (Spotify, YouTube, VLC) |
| `aux` | **Aux** | Auxiliary audio channel for system alerts or secondary apps |
| `microphone` | **Mic Monitoring** | Headphone monitor level for your microphone |

---

## 🎛️ Available Actions

### 1. Set Volume
Sets the target channel to an absolute volume percentage.
- **Channel**: Select audio channel (`Master`, `Game`, `Chat`, `Media`, `Aux`, `Microphone`).
- **Output Type**: `Classic` (default), `Streaming` (OBS/Stream), or `Monitoring` (Headphones).
- **Volume (%)**: Target volume slider from `0%` to `100%`.

### 2. Adjust Volume
Increases or decreases the volume by a specified step percentage. Ideal for rotary encoders or standard up/down buttons.
- **Channel**: Target audio channel.
- **Output Type**: `Classic`, `Streaming`, or `Monitoring`.
- **Direction**: `Increase (+)` or `Decrease (-)`.
- **Step (%)**: Volume delta per press (e.g., `5%`).

### 3. Mute / Unmute
Explicitly sets the channel's mute state to On or Off.
- **Channel**: Target audio channel.
- **Output Type**: `Classic`, `Streaming`, or `Monitoring`.
- **State**: `Mute (Off)` or `Unmute (On)`.

### 4. Toggle Mute
Flips the current mute state of the channel (if unmuted, mutes it; if muted, unmutes it).
- **Channel**: Target audio channel.
- **Output Type**: `Classic`, `Streaming`, or `Monitoring`.

### 5. Set Chat Mix
Adjusts the hardware/software Chat Mix balance slider.
- **Chat Mix Balance**: Slider from `-100` (100% Chat / Voice) through `0` (Balanced) to `+100` (100% Game).

### 6. Set Streamer Mode
Controls SteelSeries GG Sonar's Streamer Mode layout.
- **Action**: `Toggle`, `Enable`, or `Disable`.

---

## 📦 Installation

### Option 1: Macro Deck Extension Store (Recommended)
1. Open **Macro Deck 3**.
2. Go to **Extension Store** -> **Plugins**.
3. Search for **SteelSeries GG Sonar**.
4. Click **Install**.

### Option 2: Manual Installation from GitHub Releases
1. Download the latest `.macroDeckPlugin` file from the [Releases](https://github.com/Cjhackeryt/SteelseriesSonar/releases) tab.
2. In **Macro Deck 3**, navigate to **Plugins** and click **Install from file**.
3. Select the downloaded `.macroDeckPlugin` file.
4. Macro Deck will install and start the plugin automatically.

---

## 🛠️ System Requirements

| Component | Minimum Requirement |
|---|---|
| **OS** | Windows 10 (64-bit) / Windows 11 |
| **Macro Deck** | Macro Deck 3.0 or later |
| **SteelSeries GG** | Any version with the **Sonar** feature enabled |
| **Runtime** | .NET 10.0 Runtime (installed by Macro Deck if needed) |

> [!NOTE]
> SteelSeries Sonar must be active in SteelSeries GG. The plugin connects locally via SteelSeries GG's IPC REST service.

---

## 💻 Building from Source

To compile and package the plugin locally:

### 1. Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Macro Deck Plugin CLI](https://www.nuget.org/packages/MacroDeck.Plugin.Cli)

Install the Macro Deck CLI tool:
```powershell
dotnet tool install -g MacroDeck.Plugin.Cli --prerelease
```

### 2. Build the Project
```powershell
cd SteelSeriesSonarPlugin
dotnet restore
dotnet build -c Release
```

### 3. Package into `.macroDeckPlugin`
```powershell
macrodeck-plugin pack --source bin/Release/net10.0 --output ../dist/com.steelseries.sonar.macroDeckPlugin --force
```
The installable artifact will be generated in `dist/com.steelseries.sonar.macroDeckPlugin`.

---

## 🚀 Continuous Integration & Deployment

This repository includes a fully configured GitHub Actions workflow (`.github/workflows/build-and-release.yml`):
- **On Push/PR**: Automatically compiles, verifies, and packages the plugin artifact.
- **On Version Tag (`v*`)**: Automatically creates a new GitHub Release with release notes and attaches the compiled `.macroDeckPlugin` package.

---

## 🏗️ Architecture & Technical Details

```
SteelSeriesSonarPlugin/
├── Actions/                      ← Macro Deck action definitions & executors
│   ├── SetVolumeAction.cs
│   ├── AdjustVolumeAction.cs
│   ├── MuteAction.cs
│   ├── ToggleMuteAction.cs
│   ├── SetChatMixAction.cs
│   ├── SetStreamerModeAction.cs
│   └── SonarChoices.cs
├── SonarClient.cs                ← High-performance HTTP client for Sonar REST API
├── SonarIntegration.cs           ← Macro Deck IPluginIntegration lifecycle
├── Program.cs                    ← Plugin entrypoint, DI & IHttpClientFactory config
├── manifest.json                 ← Macro Deck 3 Plugin Manifest
└── nuget.config
```

### Dynamic Port Discovery
SteelSeries GG allocates a dynamic port on each launch. The plugin reads `C:\ProgramData\SteelSeries\GG\coreProps.json` to query GG's local `/subApps` registry. The discovered base URL is cached in-memory and automatically refreshed whenever a reconnection is needed.

### Connection Management
All HTTP interactions are managed via `IHttpClientFactory` with custom TLS validation for localhost certificates and connection pooling to prevent socket exhaustion.

---

## 🤝 Contributing

Contributions, bug reports, and pull requests are welcome!
1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/amazing-feature`).
3. Commit your changes (`git commit -m 'Add amazing feature'`).
4. Push to the branch (`git push origin feature/amazing-feature`).
5. Open a Pull Request.

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.
