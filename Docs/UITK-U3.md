# UI Toolkit U3

LevelEditor now uses one runtime UIDocument with `LevelEditor.uxml`, the shared `Theme.uss`, TextCore fonts and `GamePanelSettings`. Its existing Expand scaling, 1080 x 1920 reference resolution and sorting order are unchanged.

| Interface | Binding |
| --- | --- |
| Level selector, Load/New/Save/Test Play | Existing LevelEditorPanel draft and persistence APIs; selecting a level still loads immediately |
| Width/height/Apply, time, Auto Grid | TextField values passed to the existing methods; time also applies on focus loss |
| Bus order and colour buttons | AddBus/RemoveBusAt; pooled numbered chips |
| Scrollable passenger palette | Placed/required counts including tunnel queues, selection, pointer capture and one reusable drag ghost |
| Tunnel popup | TunnelSelected event, rotate/R, queue add/remove and remaining-count limits |
| Passenger popup | Hidden, ropes, cloak and delete through the existing mutually exclusive option APIs |
| Solve and Auto Fill | Existing LevelSolver/LevelAutoFill calls; the configured solution display limit remains 10+ |

LevelEditorPanel retains the draft editing, validation and save logic; its Toolkit partial owns the visual bindings. EditorBoardInput now polls the existing mouse buttons and checks UI Toolkit picking before projecting a board click. Document roots, empty layout containers and the ghost ignore picking. Controls and panel surfaces intercept their own area. Screen/panel conversion keeps palette drops aligned with the board camera.

Safe-area padding and portrait/square/landscape classes follow LayoutModeController. The top controls use two rows in portrait/square and one in landscape. Popups remain centered inside the safe area and bounded to 92%, with scrollable content. Native dropdowns receive the same stylesheet at panel level; callback and stylesheet ownership are released on disable. Elements are cached per document, chip buttons are pooled, and drag movement performs no string formatting or managed object creation.

Both legacy Canvas roots are removed, including the inactive camera-background Canvas. The active world background is unchanged. Six obsolete editor uGUI controllers plus the now-unreferenced CanvasArtwork and SafeArea scripts are removed. Game scene, gameplay systems, LevelData, solver/generator implementations, level assets, materials and world settings are unchanged.

## Verification

Unity 2022.3.19f1, Edit mode only. No Play mode or builds.

- 83 assertions on isolated UI, board and draft clones: size/time, bus counts, palette counts/selection, passenger options, tunnel queue/rotation, invalid-save rejection, solver results, Auto Fill, pointer capture/ghost cancellation, drag placement, board left/right/middle clicks, UI input blocking, safe insets, centered 92% popups and repeated enable/disable bindings.
- Existing Level 1 returns `10+ solutions found.` Saving a valid asset and Test Play were not executed because review must leave saved data and Play mode untouched.
- Offscreen runtime-panel captures of the editor and both popups at 1080 x 1920 and 1920 x 1080, plus selector/drag captures. These show UI over a flat clear color; the world camera and layout listeners were not driven by review resolutions. Square layout and asymmetric safe insets were also checked.
- After removing the temporary helper, compilation completed, scriptCompilationFailed is false, and Console has zero entries. LevelEditor was reopened: one UIDocument, zero Canvas components, zero missing scripts, all nine controller object references assigned, shared PanelSettings and UXML assigned.
- Every retained scene YAML block matches the starting revision except SceneRoots, which replaces the Canvas roots with the UI document. There are no dangling local scene references. No temporary helper, menu or capture folder remains in the repository.

Captures and detailed results are outside the repository under `C:/Users/Kullanici/Documents/BusJamCase_UITK_Review/U3/`. Runtime device input and gameplay were not exercised under the Edit mode restriction.
