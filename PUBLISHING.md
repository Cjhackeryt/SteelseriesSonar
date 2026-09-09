# 🚀 Publishing & Distribution Guide

This guide walks you through publishing the **SteelSeries GG Sonar** plugin to **GitHub** and the **Macro Deck Extension Store**.

---

## 1. Initializing & Pushing to GitHub

If you haven't published your code to GitHub yet, follow these commands in PowerShell from the project root:

```powershell
# 1. Initialize Git repository
git init

# 2. Add all files
git add .

# 3. Create your initial commit
git commit -m "feat: initial release of SteelSeries Sonar Macro Deck plugin"

# 4. Rename default branch to main
git branch -M main

# 5. Link your GitHub remote repository
git remote add origin https://github.com/Cjhackeryt/SteelseriesSonar.git

# 6. Push to GitHub
git push -u origin main
```

---

## 2. Automated Releases via GitHub Actions

The repository includes a ready-to-use GitHub Actions workflow located in [`.github/workflows/build-and-release.yml`](.github/workflows/build-and-release.yml).

### How to trigger an automated release:
Whenever you are ready to publish a new version (e.g. `v1.0.0`):

```powershell
# 1. Create a git tag matching your version
git tag v1.0.0

# 2. Push the tag to GitHub
git push origin v1.0.0
```

### What happens automatically:
1. GitHub Actions will start a Windows runner with .NET 10.
2. It restores and compiles the project in `Release` mode.
3. It installs `MacroDeck.Plugin.Cli` and validates your `manifest.json`.
4. It packages the `com.steelseries.sonar.macroDeckPlugin` binary.
5. It automatically creates a new **GitHub Release** under your repository and attaches the compiled `.macroDeckPlugin` file ready for users to download.

---

## 3. Submitting to the Macro Deck Extension Store

To make your plugin visible in the in-app **Macro Deck Extension Store**:

1. **Verify your Manifest**:
   Make sure [`SteelSeriesSonarPlugin/manifest.json`](SteelSeriesSonarPlugin/manifest.json) has your exact version, description, and author details:
   ```json
   {
     "manifestVersion": 1,
     "id": "com.steelseries.sonar",
     "name": "SteelSeries GG Sonar",
     "version": "1.0.0",
     "description": "Control SteelSeries GG Sonar audio channels — volume, mute, chat mix, and streamer mode — directly from your Macro Deck.",
     "publisher": {
       "name": "Your Name / Organization"
     },
     "icon": "Assets/icon.svg",
     "entrypoints": {
       "win-x64": {
         "executable": "SteelSeriesSonarPlugin.dll",
         "runtime": {
           "kind": "FrameworkDependent",
           "dotnetVersion": "10.0"
         }
       }
     }
   }
   ```

2. **Test your package locally**:
   ```powershell
   cd SteelSeriesSonarPlugin
   dotnet build -c Release
   New-Item -ItemType Directory -Force -Path ..\dist\staging\runtimes\win-x64
   Copy-Item bin\Release\net10.0\* ..\dist\staging\runtimes\win-x64 -Recurse -Force
   Remove-Item ..\dist\staging\runtimes\win-x64\manifest.json, ..\dist\staging\runtimes\win-x64\icon.svg -Force -ErrorAction SilentlyContinue
   Remove-Item ..\dist\staging\runtimes\win-x64\Assets -Recurse -Force -ErrorAction SilentlyContinue
   Copy-Item manifest.json,icon.svg ..\dist\staging -Force
   Copy-Item Assets ..\dist\staging -Recurse -Force
   macrodeck-plugin validate --directory ..\dist\staging
   macrodeck-plugin pack --source ..\dist\staging --output ..\dist\com.steelseries.sonar.macroDeckPlugin --force
   ```

3. **Submit to Macro Deck**:
   - Visit the [Macro Deck Extension Store Portal / GitHub Repository](https://github.com/SuchByte/Macro-Deck).
   - Submit your plugin metadata / package URL according to Macro Deck 3 extension submission instructions.

---

## 4. Manual Testing / Direct Distribution

Users can install the plugin directly without using the store:
1. Send them the generated `dist/com.steelseries.sonar.macroDeckPlugin` file (or direct them to your GitHub Releases page).
2. In Macro Deck 3, open **Plugins** -> **Install from file**.
3. Select the file and click **Install**.
