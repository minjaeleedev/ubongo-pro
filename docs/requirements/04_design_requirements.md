# Ubongo 3D Pro - Design Requirements

## 1. Visual Identity

### 1.1 Color Palette

#### Block Colors (8 Primary Colors)
| Block ID | Color Name | Hex Code | RGB | Description |
|----------|------------|----------|-----|-------------|
| 1 | Sunset Orange | #FF6B35 | 255, 107, 53 | Warm, energetic orange |
| 2 | Ocean Blue | #2E86AB | 46, 134, 171 | Deep, calming blue |
| 3 | Jungle Green | #28A745 | 40, 167, 69 | Vibrant tropical green |
| 4 | Royal Purple | #7B2CBF | 123, 44, 191 | Rich, regal purple |
| 5 | Savanna Yellow | #FFD93D | 255, 217, 61 | Bright, sunny yellow |
| 6 | Coral Pink | #FF6B9D | 255, 107, 157 | Playful, warm pink |
| 7 | Turquoise | #17A2B8 | 23, 162, 184 | Fresh, aquatic teal |
| 8 | Earth Brown | #8B5A2B | 139, 90, 43 | Natural, grounded brown |

#### UI Colors
| Usage | Color Name | Hex Code | RGB |
|-------|------------|----------|-----|
| Primary Background | Warm Sand | #F5E6D3 | 245, 230, 211 |
| Secondary Background | Deep Mahogany | #3D2914 | 61, 41, 20 |
| Accent | Golden Sun | #FFB800 | 255, 184, 0 |
| Text Primary | Charcoal | #2D2D2D | 45, 45, 45 |
| Text Secondary | Warm Gray | #6B6B6B | 107, 107, 107 |
| Success | Safari Green | #4CAF50 | 76, 175, 80 |
| Warning | Amber Alert | #FF9800 | 255, 152, 0 |
| Error | Crimson | #DC3545 | 220, 53, 69 |
| Timer Normal | Sky Blue | #4FC3F7 | 79, 195, 247 |
| Timer Warning | Warning Orange | #FF7043 | 255, 112, 67 |
| Timer Critical | Alert Red | #EF5350 | 239, 83, 80 |

### 1.2 Font Style

| Usage | Font Family | Weight | Size |
|-------|-------------|--------|------|
| Title/Logo | Bangers (Google Fonts) | Regular | 48-72px |
| Headings | Nunito | Bold (700) | 24-36px |
| Body Text | Nunito | Regular (400) | 16-20px |
| UI Labels | Nunito | SemiBold (600) | 14-18px |
| Timer Display | Orbitron | Bold (700) | 32-48px |
| Score Display | Orbitron | Medium (500) | 24-32px |

### 1.3 Icon Style

- **Style**: Rounded, friendly, slightly playful
- **Stroke Width**: 2-3px for outlined icons
- **Corner Radius**: 4px minimum
- **Color**: Monochrome with accent color highlights
- **Size Standards**: 24x24px (small), 32x32px (medium), 48x48px (large)

---

## 2. 3D Block Design

### 2.1 Block Shapes and Colors

| Block Name | Shape | Unit Count | Color | Description |
|------------|-------|------------|-------|-------------|
| I-Block | Straight line | 4 units | Sunset Orange | 1x1x4 linear piece |
| L-Block | L-shape | 4 units | Ocean Blue | 3x1 + 1x1 corner |
| T-Block | T-shape | 4 units | Jungle Green | Cross with 3 horizontal, 1 vertical |
| Z-Block | Zigzag | 4 units | Royal Purple | 2x1 offset staircase |
| S-Block | Reverse Zigzag | 4 units | Savanna Yellow | Mirror of Z-Block |
| O-Block | Square | 4 units | Coral Pink | 2x2 flat square |
| J-Block | Reverse L | 4 units | Turquoise | Mirror of L-Block |
| Corner-Block | 3D Corner | 3 units | Earth Brown | L-shape in 3D space |

### 2.2 Block Textures and Materials

#### Material Properties
```
Shader: Standard (Specular setup)
Albedo: Block color with 10% desaturation
Smoothness: 0.7 (glossy but not mirror-like)
Metallic: 0.1 (subtle metallic sheen)
Normal Map: Subtle surface texture (noise-based)
Emission: None (except for selected state)
```

#### Material States
| State | Modification |
|-------|--------------|
| Default | Base material as described |
| Hover | +15% brightness, subtle glow outline |
| Selected | Emission enabled (30% intensity), pulsing |
| Placed | -5% saturation, slight transparency (0.95 alpha) |
| Invalid | Red tint overlay, 50% transparency |
| Locked | Grayscale, 70% transparency |

### 2.3 Block Edge Treatment

- **Edge Style**: Beveled/Rounded
- **Bevel Radius**: 0.05 units (5% of block unit size)
- **Bevel Segments**: 3 (smooth appearance without heavy poly count)
- **Edge Highlight**: Subtle lighter color on edges (+10% brightness)

### 2.4 Block Size Ratios

```
Base Unit Size: 1.0 x 1.0 x 1.0 Unity units
Visual Block Size: 0.95 x 0.95 x 0.95 (5% gap for grid visibility)
Collision Size: 0.98 x 0.98 x 0.98 (slight tolerance)
Grid Spacing: 1.0 units
```

---

## 3. Game Board Design

### 3.1 Board Grid Visualization

#### Grid Properties
```
Grid Size: Variable (based on puzzle difficulty)
- Easy: 3x3 to 4x4
- Medium: 4x4 to 5x5
- Hard: 5x5 to 6x6

Cell Size: 1.0 x 1.0 Unity units
Grid Line Color: #4A4A4A (Dark Gray)
Grid Line Width: 0.02 units
Grid Background: #E8DCC8 (Light Tan)
```

#### Grid Visual Layers
1. **Base Layer**: Solid background color
2. **Grid Lines**: Thin dark lines separating cells
3. **Cell Highlights**: Subtle gradient from center
4. **Target Overlay**: Semi-transparent target shape

### 3.2 Target Area Display

| Element | Style |
|---------|-------|
| Target Outline | 3px solid #FFB800 (Golden Sun) |
| Target Fill | #FFB800 at 20% opacity |
| Target Glow | Animated pulse, 0.5s cycle |
| Valid Placement | Green highlight (#4CAF50 at 30%) |
| Invalid Area | Red crosshatch pattern |

### 3.3 Layer Separation Display

```
Layer Height: 1.0 units
Layer Gap (Visual): 0.1 units
Layer Indicator Colors:
  - Layer 1 (Bottom): #8B7355 (Tan)
  - Layer 2 (Middle): #A0896E (Light Tan)
  - Layer 3 (Top): #B8A588 (Cream)

Layer Selection UI:
  - Active Layer: Full opacity, highlighted border
  - Inactive Layer: 50% opacity, no border
  - Layer Tabs: Side panel, vertical arrangement
```

---

## 4. Gem Design

### 4.1 Gem Types (4 Varieties)

| Gem Type | Color Name | Hex Code | Shape | Facets |
|----------|------------|----------|-------|--------|
| Ruby | Crimson Red | #E31C3D | Oval Cut | 16 facets |
| Sapphire | Royal Blue | #1A5FB4 | Round Brilliant | 24 facets |
| Emerald | Forest Green | #2E7D32 | Emerald Cut | 12 facets |
| Amber | Golden Orange | #FFB300 | Cushion Cut | 18 facets |

### 4.2 Gem Visual Properties

```
Size: 0.3 x 0.3 x 0.2 Unity units (icon size)
Material: Transparent with high refraction
  - Transparency: 0.85
  - Index of Refraction: 1.5
  - Specular: 0.9
  - Smoothness: 0.95

Inner Glow: Subtle emission from center
Sparkle Effect: Random highlight flashes (particle system)
```

### 4.3 Gem Icons (2D)

```
Icon Size: 64x64px (base), scalable
Style: Flat design with subtle gradient
Border: 2px white stroke with drop shadow
Background: Transparent or circular backing
```

| Gem | Icon Description |
|-----|------------------|
| Ruby | Red oval with white highlight, warm gradient |
| Sapphire | Blue circle with star highlight, cool gradient |
| Emerald | Green rectangle with corner cuts, natural gradient |
| Amber | Orange rounded square, warm honey gradient |

### 4.4 Gem Acquisition Animation

```
Duration: 1.2 seconds total

Phase 1 (0-0.3s): Pop
  - Scale from 0.5 to 1.2
  - Rotation: 0 to 360 degrees (Y-axis)
  - Position: Original to +0.5 units up

Phase 2 (0.3-0.8s): Float and Shine
  - Scale: 1.2 to 1.0
  - Sparkle particle burst (12 particles)
  - Gentle bob animation

Phase 3 (0.8-1.2s): Collect
  - Move to UI gem counter position
  - Scale down to icon size
  - Fade trail effect
  - Sound: Chime + collection sound
```

---

## 5. UI Layout Design

### 5.1 Main Menu Layout

```
+--------------------------------------------------+
|                    LOGO AREA                      |
|              [Ubongo 3D Pro Logo]                 |
|                  (centered)                       |
+--------------------------------------------------+
|                                                   |
|              [  PLAY  ]  (Primary Button)         |
|                                                   |
|              [ PUZZLE SELECT ]                    |
|                                                   |
|              [ SETTINGS ]                         |
|                                                   |
|              [ HOW TO PLAY ]                      |
|                                                   |
+--------------------------------------------------+
|  [Sound]  [Music]                    [Quit Game] |
+--------------------------------------------------+

Button Style:
  - Width: 300px
  - Height: 60px
  - Border Radius: 12px
  - Primary: Golden Sun (#FFB800)
  - Secondary: Warm Sand (#F5E6D3)
  - Hover: Scale 1.05, brightness +10%
```

### 5.2 Game Screen Layout

```
+--------------------------------------------------+
|  Timer: 02:30  |  Score: 1250  |  Level: 3/20    |
+--------------------------------------------------+
|                                                   |
|     +------------------+    +----------------+    |
|     |                  |    |   AVAILABLE    |    |
|     |   3D GAME BOARD  |    |    BLOCKS      |    |
|     |   (Main View)    |    |   (Inventory)  |    |
|     |                  |    |                |    |
|     |   [Rotate View]  |    |  [B1] [B2]     |    |
|     |                  |    |  [B3] [B4]     |    |
|     +------------------+    +----------------+    |
|                                                   |
+--------------------------------------------------+
|  [Hint]  [Undo]  [Reset]           [Pause/Menu]  |
+--------------------------------------------------+

Camera Controls:
  - Left Click + Drag: Rotate view
  - Right Click + Drag: Pan
  - Scroll: Zoom in/out
  - Double Click: Reset view

HUD Elements:
  - Timer: Top-left, large digital display
  - Score: Top-center, with gem icons
  - Level Progress: Top-right, progress bar
  - Block Inventory: Right panel, scrollable
  - Action Buttons: Bottom bar, fixed position
```

### 5.3 Result Screen Layout

```
+--------------------------------------------------+
|                  PUZZLE COMPLETE!                 |
|                     [Stars]                       |
+--------------------------------------------------+
|                                                   |
|              Time: 01:45 / 03:00                  |
|              Score: +500 pts                      |
|                                                   |
|     +------------------------------------------+ |
|     |  Gems Earned:  [Ruby] [Sapphire] [+2]    | |
|     +------------------------------------------+ |
|                                                   |
|              Total Score: 1750 pts               |
|                                                   |
+--------------------------------------------------+
|                                                   |
|    [ NEXT PUZZLE ]        [ REPLAY ]              |
|                                                   |
|              [ MAIN MENU ]                        |
|                                                   |
+--------------------------------------------------+

Star Rating:
  - 3 Stars: Under 50% time used
  - 2 Stars: Under 75% time used
  - 1 Star: Completed within time limit
  - 0 Stars: Time expired (puzzle not solved)
```

---

## 6. Animation and Effects

### 6.1 Block Placement Animation

```
Pickup Animation (0.2s):
  - Scale: 1.0 to 1.1
  - Y Position: +0.1 units
  - Shadow: Expand and soften
  - Sound: Soft "click" sound

Drag Animation:
  - Block follows cursor/touch with slight lag (lerp 0.15)
  - Ghost preview at valid placement positions
  - Invalid positions show red tint

Drop Animation (0.3s):
  - Scale: 1.1 to 0.95 to 1.0 (bounce)
  - Y Position: Drop to grid level
  - Particle burst: 6 small particles
  - Sound: Satisfying "thunk" sound
  - Camera: Subtle shake (0.02 units, 0.1s)
```

### 6.2 Puzzle Complete Effect

```
Duration: 2.5 seconds

Phase 1 - Flash (0-0.3s):
  - Screen flash: White at 30% opacity
  - All blocks glow simultaneously

Phase 2 - Celebration (0.3-1.5s):
  - Confetti particle system (50 particles)
  - Blocks pulse outward in sequence
  - Stars animate in from edges
  - "Complete!" text scales in with bounce

Phase 3 - Score Tally (1.5-2.5s):
  - Numbers count up animation
  - Gems fly to counter
  - Final score display with glow

Sound Sequence:
  - 0.0s: Success fanfare
  - 0.3s: Confetti sound
  - 1.5s: Counting tick sounds
  - 2.3s: Final "ding"
```

### 6.3 Timer Warning Effects

```
Normal State (> 30 seconds):
  - Color: Sky Blue (#4FC3F7)
  - Animation: None
  - Sound: None

Warning State (10-30 seconds):
  - Color: Warning Orange (#FF7043)
  - Animation: Gentle pulse (scale 1.0-1.05, 1s cycle)
  - Sound: Soft tick every 5 seconds

Critical State (< 10 seconds):
  - Color: Alert Red (#EF5350)
  - Animation: Rapid pulse (scale 1.0-1.1, 0.5s cycle)
  - Sound: Tick every second, increasing urgency
  - Screen: Subtle red vignette at edges

Time Expired:
  - Flash: Red screen flash
  - Sound: Buzzer sound
  - Animation: Timer shakes and fades
```

### 6.4 Transition Animations

```
Screen Fade (Default):
  - Duration: 0.5s
  - Easing: Ease-in-out
  - Color: Black

Menu to Game:
  - Duration: 0.8s
  - Effect: Zoom into board while fading
  - Sound: Whoosh sound

Game to Result:
  - Duration: 0.6s
  - Effect: Blur and scale up result panel
  - Sound: Success/failure jingle

Level Transition:
  - Duration: 1.0s
  - Effect: Current puzzle slides out, new slides in
  - Progress bar animation
```

---

## 7. Sound Design Guidelines

### 7.1 Background Music Style

```
Genre: African-inspired world music fusion
Tempo: 90-110 BPM (upbeat but not frantic)
Instruments:
  - Primary: Kalimba, marimba, djembe drums
  - Secondary: Soft synth pads, bass
  - Accents: Shaker, wood blocks

Mood Variations:
  - Menu: Relaxed, welcoming (90 BPM)
  - Gameplay: Energetic, focused (100 BPM)
  - Time Warning: Intensified, urgent (110 BPM)
  - Victory: Celebratory, triumphant
  - Failure: Sympathetic, encouraging

Dynamic Music System:
  - Layers added/removed based on game state
  - Smooth crossfades between variations
  - Volume: 40-60% (doesn't overpower SFX)
```

### 7.2 Sound Effects List

#### UI Sounds
| Action | Sound Description | Duration |
|--------|-------------------|----------|
| Button Hover | Soft wooden tap | 0.1s |
| Button Click | Satisfying click with resonance | 0.15s |
| Menu Open | Whoosh + settle | 0.3s |
| Menu Close | Reverse whoosh | 0.25s |
| Toggle On | High-pitched click | 0.1s |
| Toggle Off | Low-pitched click | 0.1s |

#### Gameplay Sounds
| Action | Sound Description | Duration |
|--------|-------------------|----------|
| Block Pickup | Light lift sound | 0.15s |
| Block Place (Valid) | Satisfying thunk | 0.2s |
| Block Place (Invalid) | Dull thud + error tone | 0.3s |
| Block Rotate | Mechanical click | 0.1s |
| Block Return | Slide + settle | 0.25s |
| Undo Action | Reverse swoosh | 0.2s |
| Hint Used | Magical chime | 0.4s |

#### Feedback Sounds
| Event | Sound Description | Duration |
|-------|-------------------|----------|
| Puzzle Complete | Triumphant fanfare | 1.5s |
| Star Earned | Sparkle + chime | 0.3s |
| Gem Collected | Crystal ring | 0.4s |
| Time Warning (30s) | Soft alert tone | 0.3s |
| Time Warning (10s) | Urgent tick | 0.2s |
| Time Expired | Gentle buzzer | 0.5s |
| Level Up | Achievement sound | 0.8s |
| New High Score | Celebration jingle | 1.2s |

#### Ambient Sounds
| Context | Sound Description |
|---------|-------------------|
| Menu Ambient | Soft African nature sounds (birds, wind) |
| Gameplay Ambient | Subtle environmental hum |
| Victory Screen | Gentle celebration ambience |

---

## 8. Testbed UI Specifications

### 8.1 Debug Information Display

```
+--------------------------------------------------+
|  DEBUG PANEL (Toggle with F12)                   |
+--------------------------------------------------+
|  FPS: 60  |  Draw Calls: 45  |  Triangles: 12.5K |
+--------------------------------------------------+
|  Current Puzzle: puzzle_001                       |
|  Difficulty: Medium                               |
|  Target Shape: 4x3x2                              |
|  Blocks Available: 5/5                            |
|  Blocks Placed: 3/5                               |
+--------------------------------------------------+
|  Grid Position: (2, 1, 0)                         |
|  Selected Block: L-Block                          |
|  Rotation State: (90, 0, 0)                       |
|  Valid Placement: TRUE                            |
+--------------------------------------------------+
|  Solver Status: SOLVED (2.3s)                     |
|  Solutions Found: 3                               |
|  Current Solution: 1/3                            |
+--------------------------------------------------+

Style:
  - Background: #000000 at 80% opacity
  - Font: Monospace, 12px
  - Color: #00FF00 (green terminal style)
  - Position: Top-left corner
  - Collapsible sections
```

### 8.2 Puzzle Generation Test Tools

```
+--------------------------------------------------+
|  PUZZLE GENERATOR TEST PANEL                      |
+--------------------------------------------------+
|  Difficulty: [Easy] [Medium] [Hard] [Custom]      |
|                                                   |
|  Grid Size:                                       |
|    Width:  [3] [4] [5] [6]                        |
|    Height: [3] [4] [5] [6]                        |
|    Depth:  [1] [2] [3]                            |
|                                                   |
|  Block Count: [3] [4] [5] [6] [7] [8]             |
|                                                   |
|  Block Types: (checkboxes)                        |
|    [x] I-Block  [x] L-Block  [x] T-Block          |
|    [x] Z-Block  [x] S-Block  [x] O-Block          |
|    [x] J-Block  [x] Corner                        |
|                                                   |
|  [ GENERATE PUZZLE ]  [ VALIDATE ]  [ SOLVE ]    |
|                                                   |
|  Generation Log:                                  |
|  > Generating puzzle...                           |
|  > Placing blocks...                              |
|  > Validating solution...                         |
|  > Puzzle valid: YES                              |
|                                                   |
|  [ EXPORT JSON ]  [ IMPORT JSON ]  [ CLEAR ]     |
+--------------------------------------------------+
```

### 8.3 Block Rotation Test Interface

```
+--------------------------------------------------+
|  BLOCK ROTATION TEST                              |
+--------------------------------------------------+
|                                                   |
|     +----------------+                            |
|     |                |                            |
|     |   3D BLOCK     |     Current Rotation:      |
|     |   PREVIEW      |     X: 90   Y: 0   Z: 0    |
|     |                |                            |
|     +----------------+                            |
|                                                   |
|  Block Selection:                                 |
|  [I] [L] [T] [Z] [S] [O] [J] [Corner]            |
|                                                   |
|  Rotation Controls:                               |
|  X-Axis: [+90] [-90] | Current: 90               |
|  Y-Axis: [+90] [-90] | Current: 0                |
|  Z-Axis: [+90] [-90] | Current: 0                |
|                                                   |
|  [ RESET ROTATION ]  [ RANDOM ROTATION ]         |
|                                                   |
|  Rotation States (24 unique):                    |
|  [ ] Show all rotation states                    |
|  Current State Index: 4/24                       |
|                                                   |
|  Collision Test:                                 |
|  [ ] Show collision bounds                       |
|  [ ] Show pivot point                            |
|  [ ] Show grid snapping                          |
|                                                   |
|  Output Log:                                      |
|  > Block: L-Block                                 |
|  > Rotation: (90, 0, 0)                          |
|  > Bounds: (2, 1, 2)                             |
|  > Valid positions: 12                           |
+--------------------------------------------------+
```

### 8.4 Keyboard Shortcuts (Testbed)

| Key | Action |
|-----|--------|
| F1 | Show/Hide Help Overlay |
| F2 | Toggle Debug Panel |
| F3 | Toggle Puzzle Generator |
| F4 | Toggle Rotation Tester |
| F5 | Quick Generate Puzzle |
| F6 | Auto-Solve Current Puzzle |
| F7 | Step Through Solution |
| F8 | Export Current State |
| F9 | Import State from Clipboard |
| F10 | Toggle Grid Overlay |
| F11 | Toggle Wireframe Mode |
| F12 | Toggle Performance Stats |
| Ctrl+Z | Undo Last Action |
| Ctrl+Y | Redo Action |
| Ctrl+R | Reset Puzzle |
| Ctrl+N | New Random Puzzle |

---

## Appendix A: Asset Naming Conventions

```
Textures:
  T_[Object]_[Type]_[Variant]
  Example: T_Block_Albedo_Orange, T_UI_Button_Normal

Materials:
  M_[Object]_[Variant]
  Example: M_Block_Orange, M_Gem_Ruby

Prefabs:
  PF_[Category]_[Name]
  Example: PF_Block_LShape, PF_UI_Button

Animations:
  AN_[Object]_[Action]
  Example: AN_Block_Pickup, AN_Gem_Collect

Audio:
  SFX_[Category]_[Name]
  BGM_[Context]_[Variant]
  Example: SFX_UI_Click, BGM_Gameplay_Normal

UI Elements:
  UI_[Screen]_[Element]
  Example: UI_Menu_PlayButton, UI_Game_Timer
```

## Appendix B: Color Accessibility

All color combinations meet WCAG 2.1 AA standards:
- Text on backgrounds: Minimum 4.5:1 contrast ratio
- Large text/icons: Minimum 3:1 contrast ratio
- Interactive elements: Clear visual distinction in all states

Alternative color modes planned:
- High Contrast Mode
- Colorblind-friendly Mode (Deuteranopia, Protanopia, Tritanopia)
