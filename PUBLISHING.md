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

The repository uses [`.github/workflows/release.yml`](.github/workflows/release.yml) to build and upload packages to the Macro Deck Creator Portal, and [`.github/workflows/attach-release-asset.yml`](.github/workflows/attach-release-asset.yml) to attach the package to the corresponding GitHub Release.

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
2. The Macro Deck reusable workflow restores, compiles, validates, and packages the plugin.
3. The package is uploaded to the Macro Deck Creator Portal and retained as a GitHub Actions artifact.
4. The asset workflow attaches the generated `.macroDeckPlugin` file to the matching GitHub Release.

---

## 3. Submitting to the Macro Deck Extension Store

The repository contains the `macrodeck-build.json` and
`.github/workflows/release.yml` files required by the
[Macro Deck Creator Portal](https://docs.macro-deck.app/creator-portal/).
The portal accepts only builds uploaded by its reusable GitHub workflow.

1. Sign in to the Creator Portal and create a **Plugin / Integration** project.
   Use `com.ckhackeryt.steelseriessonar` as the Package ID; it must match the `id` in
   [`SteelSeriesSonarPlugin/manifest.json`](SteelSeriesSonarPlugin/manifest.json).
2. In the project's **Builds** page, connect the public
   `Cjhackeryt/SteelseriesSonar` GitHub repository.
3. Commit and push the publishing workflow and build definition if they are not
   already on GitHub.
4. Create and push a semantic version tag, for example:
   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```
   The workflow builds and uploads the package to the Creator Portal. The tag
   version is written into the manifest by the publishing workflow.
5. In the portal, create a release from the build, add it to the submission,
   complete the Store summary and tags, and select **Submit for Review**.

The Macro Deck Store signs the package after approval. No signing key or
publishing secret is required.

---

## 4. Manual Testing / Direct Distribution

Users can install the plugin directly without using the store:
1. Send them the generated `dist/com.ckhackeryt.steelseriessonar.macroDeckPlugin` file (or direct them to your GitHub Releases page).
2. In Macro Deck 3, open **Plugins** -> **Install from file**.
3. Select the file and click **Install**.
