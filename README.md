# 🧩 3D Tileset-based Model Synthesis - Unity Project

Welcome to the **Model Synthesis** Unity project! This tool allows you to **create modular tilesets** and **synthesize complex room layouts** based on customizable rulesets.
Use it to either procedurally generate rooms at runtime or to curate rooms in the editor.

---

## 🎮 Features

- 🧱 **Tileset Authoring** – Design modular pieces that fit together seamlessly.
- ⚙️ **Ruleset System** – Define spatial rules for how tiles are placed. Define the neighbours allowed, custom size, rotations, and more!
- 🧠 **Model Synthesis Algorithm** – Effeciently generates room layouts from your tiles. Cannot fail as long as the rulesets are not too strict.
- 🛠️ Supports pre-placing tiles using _Preplaced Tile_ component to starts the generation with an initial look.
- 📦 Custom inspectors to easily define the rules and tilesets. 
- 🔁 Supports both runtime and editor-time generation.

---

## 🖼️ Previews

### 🔄 Room Generation

| Generate Rooms |
|--------|
| ![Synthesise](https://github.com/user-attachments/assets/df57c07e-45e9-4525-9964-202af32af3ac) |

| Change Tilesets|
|--------|
|![Change TIleset](https://github.com/user-attachments/assets/5111d961-6486-4cf9-a0a7-6db37a1c31af) |


### 🏗️ Example Tilesets

| Tileset Preview | Supports Custom Sizes and Tiles |
|------------------|------------------|
| ![End Transmission Tileset](https://github.com/user-attachments/assets/e953cf40-c7cb-4fb4-b748-27d61b7b415e) | ![Custom](https://github.com/user-attachments/assets/97c25460-29a4-4dd9-a6ba-8cc0ee6a9bd2) |

---

## 🛠️ Getting Started

### ✅ Requirements

- Unity 6000.0.44f1 LTS or later

### 📦 Installation

Clone this repository:
   ```bash
   git clone https://github.com/yourusername/model-synthesis-unity.git
   ```
### 📚 Usage
1. Create a Tileset
Use the included Scriptable Object _Tileset_ to define your Tilset using Prefabs with the _Tile_ component.

2. Define Rules
Create a GameObject and attach the _SynthesiseController_ component. 

3. Generate Rooms
You can now generate rooms after assigning your _Tileset_.

### 📂 Project Structure
```
Assets/
├── Art/                 # Materials, FBX files, etc. Note: Included FBX files are only to be used for experimenting and as example. They are _not_ to be distributed and are used here with permission.
├── Scenes/              # Includes a default test scene
├── Scripts/             # Project logic
├── Tiles/
│   ├── Default          # Example Tileset 1
│   ├── EndTransmission  # Example Tileset 2+3 from the game "End Transmission?"
│   │   ├── Inside
│   │   └── Town
│   └── Knots            # Example Tileset 4 (2D only)
├── Tilesets/            # The defined tilesets
...
```

### 🤝 Contributing
Please feel free to open issues, suggest features, or create pull requests.

### 👋 Contact
Created by @Dreunin and @BLumbye.
