# CLAUDE.md — DECRYPTED (TSA VR Nationals 2026)

Context for Claude Code. Read this first every session. It is the durable map of
the project; the live to-do state lives at the bottom under **Current Work**.

---

## 1. What this is

**DECRYPTED — A Walk Through the History of Secret Writing.** A linear,
single-player **VR museum for Meta Quest** teaching the history of cryptography
through four hands-on exhibits. The player is auto-toured by a *Demo Director*
(no manual locomotion needed). It runs **in Unity** and deploys to a real Quest
headset.

- **Unity:** 2022.3.62f3 · Universal Render Pipeline · XR Interaction Toolkit 2.6.5 · Oculus XR Plugin 4.5.4
- **Target:** Meta Quest standalone, Android/ARM64, IL2CPP, 72 FPS, baked lighting
- **Developer:** Rishabh Patel · **Collaborator:** Tejas Karusala (museum dressing system)
- **Closing plaque (verbatim — never change):**
  > *"From Caesar's alphabet shifts to modern digital security, cryptography protects information by transforming meaning into secrets only the intended recipient can reveal."*

**Flow:** Splash → Atrium → Ancient Room (Caesar disk) → WWII Room (Enigma) → Vault Room → Reveal Chamber → Complete

---

## 2. CRITICAL — repo layout gotcha (read before editing anything)

There are **TWO clones of the same git repo** on this machine, both at the same
commit and the same `origin` (`github.com/kash2nasty/TSA-VR-Nationals-2026-2026-06-11_11-00-28`):

| Path | Role |
|---|---|
| `C:/Users/patel/TSA-VR-Nationals-2026/` (PARENT) | **The live Unity project.** Has `Library/`, `Builds/`, `decrypted_main.apk`, `UserSettings/`. This is what the Unity Editor opens. |
| `C:/Users/patel/TSA-VR-Nationals-2026/TSA-VR-Nationals-2026-2026-06-11_11-00-28/` (INNER) | **Claude's working directory (CWD).** A plain second clone (not a worktree — separate `.git`). |

**Consequence:** files I edit in the INNER clone are **NOT seen by Unity** (which
opens the PARENT) until they are synced. Sync happens via git (commit/push from
one, pull in the other) — there is no shared filesystem link.

**Before doing source edits that the user needs to see in Unity, confirm with the
user which clone they want me to edit, or whether to edit the parent directly.**
For pure code/tooling changes that will be committed and pulled, editing the
inner clone is fine. (See open question Q1 at the bottom.)

---

## 3. Architecture (how it interacts)

One Unity scene: `Assets/_Project/Scenes/Decrypted_Main.unity`. No additive scene
loading at runtime (avoids VR hitches). Each "room" is a child hierarchy toggled on/off.

**Event-driven, single source of truth for state:**

- `GameManager` (Core/) — the FSM. Owns `MuseumState` (Boot→Splash→Atrium→AncientRoom→WWIIRoom→VaultRoom→RevealChamber→Complete). Forward-only, guarded. Gated rooms (Ancient/WWII/Vault) require their puzzle solved before advancing — **Demo Mode bypasses the gate**. `[DefaultExecutionOrder(-100)]`, a `Singleton`.
- `SceneController` (Core/) — physical presentation: activates the target room, deactivates others, places the XR rig at the room's `playerAnchor`, runs the screen fade. Holds the `List<RoomDescriptor>` (per-room: roomRoot, playerAnchor, reflectionProbe, ambientKey, mixer snapshot).
- `DemoDirector` (Core/) — when `GameManager.DemoMode` is on, scripts the puzzle input for each room with human-paced dwell times. Listens to `RoomEnteredEvent`, calls each exhibit's `AutoSolve()`/`AutoEnter()`/`BeginReveal()`. Auto-resolves exhibit refs via `FindObjectOfType`.
- `EventBus` (Core/) — static pub/sub. Key events in `GameEvents.cs`: `StateChangedEvent`, `RoomEnteredEvent`, `ExperienceStartedEvent`, `ExhibitSolvedEvent`, `ShowHintEvent`.
- `SaveSystem` (Core/) — persists furthest state + solved rooms (restore-on-launch is OFF by default).

**Wiring contract (don't break):** `Managers` GameObject holds `SceneController` (needs XR Origin transform, Main Camera, 6 rooms) and `GameManager` (needs SceneController ref, Demo Mode checked). Exhibit wiring is in §6.

**Namespaces:** `Decrypted.Core`, `Decrypted.Interaction`, `Decrypted.Managers`, `Decrypted.Visuals`, `Decrypted.Util`.

---

## 4. Full script inventory (40 .cs files — corrects the old handoff's "34")

`Assets/_Project/Scripts/`

- **Core/**: `GameManager`, `SceneController`, `DemoDirector`, `EventBus`, `GameEvents`, `GameState` (MuseumState enum + RoomDescriptor), `SaveSystem`
- **Interaction/**: `CaesarCipherController`, `EnigmaController`, `EnigmaMachine`, `EnigmaRotor`, `EnigmaKeyboard`, `EnigmaLampboard`, `EnigmaLeverPull`, `VaultController`, `VaultKeypad`, `VaultKeypadButton`, `FinalRevealController`, `XRGrabTwistDisk`, `PokeButton`, `SplashScreenController`, `TutorialCard`
- **Managers/**: `AudioManager`, `InteractionManager`, `PerformanceManager`, `UIManager`
- **Visuals/**: `ScreenFader`, `RoomActivator`, `PlaqueController`, `SignalTraceRenderer`, `MuseumAmbience` ⚠️ (this file is in **Visuals/**, NOT Editor/Museum/ as the old handoff said)
- **Util/**: `Singleton`, `AudioSynth`
- **Editor/**: `BuildConfigurator`, `SceneBuilder`, `EnigmaRebuilder`
- **Editor/Museum/** (Tejas's system): `MuseumBuilder`, `MuseumContent`, `MuseumKit`, `MuseumProps`

---

## 5. Editor menu commands (`DECRYPTED ▸ …`)

| Menu | Script | Purpose |
|---|---|---|
| `Build ▸ Configure for Quest` | BuildConfigurator | Apply player/quality/build settings |
| `Build ▸ Print Setup Checklist` | BuildConfigurator | Print XR/URP assignment checklist |
| `Build Scene Skeleton` | SceneBuilder | Stamp manager rig + 6 room roots |
| `Museum ▸ Build Full Museum` (Ctrl+Shift+M) | MuseumBuilder | Build grand dressing in all 6 rooms |
| `Museum ▸ Clear Generated Dressing` | MuseumBuilder | Remove all dressing (safe, reversible) |
| `Museum ▸ Rebuild One ▸ Selected Room` | MuseumBuilder | Rebuild just the selected room |
| `Rebuild Enigma Layout` | EnigmaRebuilder | Re-lay Enigma parts |

**Museum system:** finds rooms by exact name `"Room_" + state`. Missing/mismatched
name → that gallery is silently skipped (check Console for `[Museum] Room_X not found — skipped`).
Everything it builds lives under a `MuseumDressing` child per room — purely
additive/reversible. Provides a `HERO_ANCHOR_DropInteractiveExhibitHere` empty at
local (0, 1.0, 2.0) where the real interactive exhibit should be centered.

---

## 6. Puzzle data + exhibit wiring (NEVER change the puzzle values)

| Exhibit | Values |
|---|---|
| Caesar disk | ciphertext `FURVV WKH UXELFRQ`, shift **+3** → `CROSS THE RUBICON` |
| Enigma | key `MAC`, `ZLDFDQO` → `VICTORY` (reciprocal teaching cipher, not bit-exact historical) |
| Vault | passphrase `VICTORY` (auto-sourced from Enigma in code — can't drift) |

**Wiring** (per exhibit root, fields → child objects):
- **CipherDisk** (Room_AncientRoom): `XRGrabTwistDisk` on `InnerDisk` (26 detents); `CaesarCipherController` on root → Disk=InnerDisk.
- **Enigma** (Room_WWIIRoom): `EnigmaMachine` + `EnigmaController` on root (Rotors[0/1/2], Lever). Each `Rotor_N` has `XRGrabTwistDisk` + `EnigmaRotor` with a **distinct Rotor Index 0/1/2**. `EnigmaLeverPull` on `Lever`.
- **Vault** (Room_VaultRoom): `VaultKeypad` (Passphrase=VICTORY) + `VaultController` (Door=Vault_Door) on root.
- **RevealSculpture** (Room_RevealChamber): `FinalRevealController` on root — auto-starts on room entry, no fields needed.

---

## 7. Asset pipelines

**Blender (3D art)** — `Tooling/Blender/`. Generators: `gen_common.py` (shared helpers
+ FBX export), `gen_cipher_disk.py`, `gen_enigma.py`, `gen_vault.py`,
`gen_reveal_sculpture.py`, `gen_architecture.py`, driven by `export_all.py`.
Output → `Assets/_Project/Art/Generated/*.fbx` (5 files: CipherDisk, Enigma, Vault,
RevealSculpture, Museum_Architecture).

Regenerate (confirmed working):
```
cd C:\Users\patel\TSA-VR-Nationals-2026\Tooling\Blender
"C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" --background --python export_all.py
```
Ends with `[export_all] all assets exported`. "polygons with more than 4 vertices /
cannot compute tangent space" warnings are harmless.

> **Session 8 fixes (verified present in gen_common.py):** `parent_keep_world()`
> helper (line ~294) parents children without the parent-inverse matrix FBX drops;
> `bake_space_transform=False` in `export_collection()` (line ~356). Together these
> fixed the "exploded artifacts" bug where Enigma rotors detached on export.
> If you ever see `AttributeError: module 'gen_common' has no attribute
> 'parent_keep_world'`, the Python files are from mismatched versions — replace all together.

**Audio** — `Tooling/Audio/synth_engine.py` + `generate_all_audio.py` (NumPy, from
scratch). Output → `Assets/_Project/Audio/{SFX,Ambient}/` (12 baked .wav). Keys:
`sfx_brass_click`, `sfx_gear_step`, `sfx_key_clack`, `sfx_lamp_on`, `sfx_lever_pull`,
`sfx_success_chime`, `sfx_vault_rumble`, `sfx_final_chord`, `amb_ancient`,
`amb_wwii`, `amb_vault`, `amb_atrium`.
```
cd Tooling/Audio && python generate_all_audio.py
```

---

## 8. Build & deploy to Quest

Settings (confirmed working): Android · ARM64 · IL2CPP · Linear color · ASTC ·
Input System (New) · **Development Build OFF** · Min API 29 · XR Plug-in Mgmt →
Oculus checked, Multiview on.

Deploy: Quest via USB-C → accept USB-debugging popup → File ▸ Build Settings →
confirm Android + `Decrypted_Main` in build + Quest as Run Device → **Build And
Run** (first build 5–15 min). If Quest absent: replug + re-accept. If build fails:
check Development Build is OFF.

Git: Git LFS for FBX + WAV. User commits via **GitHub Desktop** (Commit to main →
Push origin). **Always save the scene in Unity (Ctrl+S) before committing.**
Commit/push only when the user asks.

---

## 9. Known issues / non-issues

| Symptom | Real problem? |
|---|---|
| Yellow "Can't set foveation level" / "Symmetric Projection only Vulkan" in editor | No — Quest-only / advisory |
| Game view looks odd on 2D monitor | No — correct in headset |
| **All text mirror-flipped (signage + exhibit labels)** | **YES — open issue, collaborator fixing.** Two root causes: exhibit labels via Blender `text()` + axis conversion; museum signage via TextMeshPro in MuseumProps/Kit (negative scale / wrong-axis 180°). |
| **Enigma/Vault/RevealSculpture scattered in scene** | **YES — old broken scene copies; replace with fixed `Generated/` models.** |
| Demo Mode ends staring at skybox | **YES — Reveal room PlayerAnchor needs framing.** |
| Ancient Room `RoomActivator` ambient = `amb_atrium` | Minor — should be `amb_ancient`. Low priority. |
| Old `Museum_Architecture` in hierarchy but disabled | Expected (kept as backup). |
| Tejas's fake `Hero` active in WWII/Vault/Reveal | Expected — disabled per-room during hybrid cleanup. |

---

## 10. Hybrid museum plan (per room)

Keep Tejas's grand shells + dressing; disable his fake decorative `Hero`; keep the
real interactive exhibit as centerpiece. For each room: expand room → expand
`MuseumDressing` → uncheck `Hero` (don't delete) → confirm real exhibit present
under room root → position it near `HERO_ANCHOR` local (0,1,2) → re-frame
`PlayerAnchor` via GameObject ▸ Align With View (Y=0, rot X=0 Z=0) → optionally
disable old per-room Floor/point-lights (Tejas's Shell has its own).

**Replacing a broken exhibit:** the `Generated/` FBX is geometry only. Drag it under
the room, position at room center, re-add the controller components from §6 and
re-wire fields, disable/delete the old copy, re-frame PlayerAnchor. Child names
(Rotor_0/1/2, Lever, Vault_Door, InnerDisk) are preserved to match the scripts.

Rooms are at X = 0,30,60,90,120,150 (Splash→Reveal). Reveal was fixed from X=0→150
in Session 8 (had been stacking on Splash).

---

## 11. Verification limits (what I CAN'T check without you)

I can read/edit text files (scripts, .py, .md, .json) but I **cannot** open the
Unity Editor, see the scene graph live, run the game, or look through the headset.
Scene contents (`.unity`) are large YAML and unreliable to verify by reading.
When a claim depends on scene state, in-editor wiring, or headset behavior, I'll
ask you rather than guess. See open questions below.

---

## 12. Docs

`Documentation/00–07_*.md`: 00 Overview, 01 Unity Setup, 02 Environment Design,
03 Asset Pipeline, 04 Audio, 05 Optimization, 06 Storyboard/Recording,
07 Museum Expansion.

---

## Current Work (update this section as state changes)

**Remaining for the showcase video:**
1. Replace broken scene exhibits (Enigma, Vault, RevealSculpture) with fixed `Generated/` models + re-wire (§6, §10).
2. Finish PlayerAnchor framing — Splash, Atrium, WWII, Vault, Reveal (Ancient done & confirmed in headset).
3. Fix inverted text — signage + exhibit labels (collaborator handling).
4. Hybrid cleanup per room (§10) for WWII, Vault, Reveal (Ancient done).
5. Interior lighting tweaks; exhibit materials (some still plain gray); FX shaders (Hologram_URP, EnergyScanline_URP) on sculpture + vault.
6. Record in-headset + edit to ~2:40 showcase video.

**Done (Session 8):** Blender explosion + bake_space_transform fixes; Tejas museum
system merged; Reveal room X position fix; old Museum_Architecture disabled; Ancient
Room hybrid complete & confirmed in headset.

## Open questions for the user (Q#)
- **Q1 — Which clone do I edit?** Unity opens the PARENT; my CWD is the INNER clone (§2). For source edits you want to see in Unity, do you want me to edit the parent directly, or keep editing the inner clone and you pull? 
- **Q2 — Untrack build junk?** `DECRYPTED_BurstDebugInformation_DoNotShip/` is committed to git but is build output. I've added it to .gitignore; want me to also `git rm --cached` it so it stops being tracked?
