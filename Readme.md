# Unity Multiplayer Lobby & Gameplay System

## Overview
This project implements a **multiplayer lobby and gameplay system** in Unity using modern Unity services and Netcode for GameObjects (NGO). It supports lobby creation, room joining, player syncing, animated 3D characters, and optional voice chat through Vivox.

---

## ⚙️ Setup & Configuration

### 1. **Environment Variables (.env)**
You’ll need a `.env` file to store Unity Service credentials
  - To create `.env` file:
      `cp .env.example .env`
    you can get the required keys from unity cloud services dashboard

---

### 2. **Unity Editor Setup**
1. Install Unity 2022.3 LTS or later.
2. Open the project.
3. Enable UGS project linking (**Project Settings → Services → Link Project ID**).

---

## Technologies Used

### 1. **Unity Netcode for GameObjects (NGO)**
Used for real-time multiplayer networking.
- Handles **client-server communication**.
- Synchronizes **player positions, animations**, and gameplay data.
- Manages **NetworkObjects**, **NetworkVariables**, **NetworkTransforms**, and **NetworkAnimators**.
- Server-authoritative model ensures consistent game state and prevents cheating.

### 2. **Unity Relay Service**
Used to connect clients behind NAT/firewalls without port forwarding.
- Host allocates a **Relay server allocation**.
- Clients join via a **Relay join code**.
- Reduces latency and simplifies connection setup.

### 3. **Unity Lobby Service**
Used for matchmaking and room management.
- Supports **public** and **private (password-protected)** lobbies.
- Lobbies maintain metadata (player list, room name, relay join code).
- Real-time updates through **Lobby Events API**.

### 4. **Unity Authentication Service**
Handles user authentication using anonymous profiles.
- Each instance signs in with `AuthenticationService.Instance.SignInAnonymouslyAsync()`.
- Unique **Player IDs** are used to manage lobby membership.

### 5. **Vivox Voice Chat**
Adds channel-based **voice communication**.

### 6. **3D Model, Armature & Animation System**
- Each player prefab has a **CharacterController**, **Animator**, **NetworkAnimator**, and **NetworkTransform**.
- Animations sync automatically via server-driven parameters.

