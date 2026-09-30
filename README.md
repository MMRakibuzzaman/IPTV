# 📺 IPTV Player

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux%20%7C%20Android%20%7C%20iOS-lightgrey)
![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)

Welcome to **IPTV Player**, a sleek, modern, and lightning-fast cross-platform application for streaming your favorite live TV channels and M3U playlists. 

Whether you're on a desktop PC, a MacBook, or your mobile phone, IPTV Player delivers a premium, adaptive viewing experience.

## ✨ Key Features

* **Cross-Platform Excellence:** Run natively on Windows, macOS, Linux, Android, and iOS.
* **Smart Adaptive Bitrate (ABR):** Automatically detects your network speed and upgrades stream resolution on the fly. You'll see real-time updates directly in the quality menu (e.g., `Auto • 1080p`).
* **Distraction-Free Viewing:** Smart UI that dynamically hides controls while you're in full screen, leaving nothing but your content.
* **Instant Search:** Quickly find your favorite channels with a built-in search bar featuring a one-click 'X' clear button.
* **Hardware Acceleration:** Powered by the industry-leading VLC engine for smooth playback with minimal battery drain.

## 🚀 Installation & Downloads

Getting started is easy! You don't need to build the app from source to enjoy it. 

Head over to our [Releases page](../../releases) to download the latest auto-compiled version for your device:
* **Windows/Linux/macOS:** Download the standalone executables.
* **Android:** Download the ready-to-install `.apk`.
* **iOS:** Download the `.app` bundle.

*(Note: To trigger a fresh public release, project maintainers simply need to push a git tag like `v1.0.0` or run the workflow from the Actions tab).*

---

## 🛠️ For the Technically Curious (Architecture & Deep Dive)

Are you a developer, power user, or contributor wanting to understand how the magic happens? Here's a peek under the hood at our design philosophies and technical architecture.

### 1. The Core Stack
We didn't settle for bloated web wrappers. This application is built entirely using **.NET 10** and the **Avalonia UI** framework. This guarantees native performance and a unified codebase across all platforms. The entire workspace is orchestrated through a modern `IPTV.slnx` solution file located at the repository root.

### 2. The Media Engine
Our playback engine is powered by **LibVLCSharp**. 
* **Quality Selection:** To handle HLS adaptive streams seamlessly without the UI aggressively overwriting the engine, our `SelectedQuality` state matching relies strictly on the stream's `Url` rather than its dynamic display `Name`. 
* **Thread Safety:** We prioritize a buttery-smooth UI. Heavy media operations (like invoking `mediaPlayer.Play()`) and HLS parsing are strictly offloaded to background threads (`Task.Run()`). We carefully marshal UI updates back using `Avalonia.Threading.Dispatcher.UIThread.InvokeAsync`.

### 3. Smart UI Behaviors
Our UI aims for a "Premium Window" feel:
* **Full Screen vs. Windowed:** Our control-hiding logic is strictly locked to full-screen mode. When windowed, controls remain predictably static. 
* **Detached Navigation:** The hamburger menu remains visually detached from the title bar. 
* **State Management:** Instead of trashing and reloading collections when the channel list changes, the `PlayerViewModel` precisely toggles item states (like `IsSelected`) to guarantee zero lag when navigating massive channel lists.

### 4. Build & Deployment Pipeline
We love automation. Our CI/CD pipeline is handled entirely via **GitHub Actions** (`.github/workflows/release.yml`).
* It automatically provisions `.NET 10` workloads.
* It gracefully bypasses strict Xcode version validation (`-p:ValidateXcodeVersion=false`) to ensure smooth iOS builds on standard macOS runners.
* It guarantees that even if a single platform encounters a compilation hiccup, the pipeline will still successfully release the remaining platforms!

---

## 🤝 Contributing
Contributions, issues, and feature requests are welcome! Feel free to check the issues page.

## 📝 License
This project is licensed under the MIT License.
