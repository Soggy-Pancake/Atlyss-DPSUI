## 1.0.3
- Fix party member fill bars being given an int for the fill percentage
- Actually fixed the client ignoring hello packets if not host
	- *Im actually retarded*
- EasySettings is actually a soft dependency now
- Ignoring keybinds if in menu or chat now
- Fix field bosses sending an extra packet on death that had the damage values cleared.
	- This stopped you from being able to look at the damage values after a boss was killed
- Prevent going out of bounds when player is lower than 5th on the leaderboard.

## 1.0.2
- Fixed client ignoring hello packets if not host (I forgot to fix the check at some point)
	- *Im a bit retarded*

## 1.0.1
- Fixed packets still using static version string from recovery

## 1.0.0
- Recovered from losing the project files >~<
- **ThunderStore release!**
- Hide party UI when switching to another map
- Fixup build files so others can actually build this thing easily
- Some shenanagins in the build process now
- UI Mode switching works
- Actually stop sending packets after the boss is beaten so there's less traffic


## 0.0.2
- Force minimum window for damage calculations to at least 0.5 seconds (3 hits 1 frame apart would give crazy high dps values)
- Clear stored dps values when changing maps
- Auto add players in the boss arena radius to the boss damage list so it shows immediately.
- Hide party UI 30 seconds after killing a field boss
- Lower network spam
- Show boss kill time in solo mode
- Basic EasySettings support added
- Party UI slides in from the side *fancyyyyyy*
- Implemented hotkeys for configuring the UI
- Populate party damage values now that the UI is attempting to handle it
- Fixed UI not hiding when in menus from incremental update
- Added `transitionTime` setting


## 0.0.1
- UI hidden if player hasn't dealt damage to boss
- Local dps resets to 0 when changing maps
- Bars scale with the player with the most damage always having 100% fill and fill is calculated from that
- Get orb color for player bar color
- Hide UI when in menus
- **Config file support**

## 0.0.0
- Mod Created
- Basic local dps tracking
- CodeTalker integration
- Basic UI setup
