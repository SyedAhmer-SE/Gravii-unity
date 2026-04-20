# 🏃 GRAVI — Anti-Gravity Endless Runner

A dark, neon-themed endless runner built in Unity. Run. Jump. Survive.

## 🎮 Features

- **Endless Running** — Procedurally generated obstacles with increasing difficulty
- **Multiple Control Schemes** — Touch buttons, Swipe gestures, or Gyroscope
- **Ghost Chaser** — Get hit and slow down. Get hit again and the ghost catches you!
- **Progressive Difficulty** — Speed increases over time, obstacles move laterally, and spawn faster
- **6 Obstacle Types** — Low hurdles, medium walls, side walls, double hurdles, narrow gaps, floating barriers
- **Procedural Synthwave Music** — Dark electronic beats generated at runtime (zero audio files!)
- **Neon Cyberpunk Aesthetic** — Glowing materials, particle trails, dark atmosphere
- **Settings** — Control type, sensitivity sliders, music toggle
- **High Score** — Persistent best score tracking

## 🕹️ Controls

| Platform | Action | Control |
|----------|--------|---------|
| PC | Move Left/Right | A / D keys |
| PC | Jump | Space / W |
| PC | Slide | S |
| PC | Restart | R |
| Mobile (Buttons) | Move | On-screen L/R buttons |
| Mobile (Buttons) | Jump/Slide | On-screen buttons |
| Mobile (Swipe) | Move | Swipe Left/Right |
| Mobile (Swipe) | Jump | Tap or Swipe Up |
| Mobile (Swipe) | Slide | Swipe Down |
| Mobile (Gyro) | Move | Tilt device |

## 🛠️ Setup

1. Clone this repository
2. Open the project in **Unity 2022.3+** (or your installed version)
3. Open the main scene in `Assets/Scenes/`
4. Press **Play** in the Unity Editor

### Unity Ads (Optional)
1. Window → Package Manager → Install **Advertisement Legacy**
2. Get your Game ID from [Unity Dashboard](https://dashboard.unity3d.com)
3. Update IDs in `AGR_AdsManager.cs`
4. Uncomment the `using UnityEngine.Advertisements` lines

## 📁 Project Structure

```
Assets/AntiGravityRunner/Scripts/
├── Camera/          # Camera follow system
├── Editor/          # Editor utilities
├── Game/            # Core managers (Game, Music, Ads, Difficulty, Settings)
├── Ghost/           # Ghost chaser enemy + particles
├── Level/           # Endless ground generation
├── Obstacles/       # Obstacle spawning + behavior + visuals
├── Player/          # Player controller + collision handling
├── UI/              # Main menu, HUD, mobile buttons
└── Visual/          # Scene setup + player visual effects
```

## 📱 Building for Android

1. File → Build Settings → Switch Platform to Android
2. Player Settings → Set package name (e.g., `com.yourname.gravi`)
3. Build & Run

## 📄 License

This project is for educational and portfolio purposes.

---

**Made with Unity** 🎯
