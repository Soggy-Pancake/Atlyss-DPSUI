## 1.1.1

- Tripple 1s!
- Fix exceptions when searching for field bosses in custom maps

## 1.1.0
- BEEG changes to networking
	- Switched to my own fork of codetalker that supports sending binary packets with a similar setup to standard packets
		- **Complaints about packet size should stop now**
	- Dedupe player info in packets 
		- the damage lists now reference player index in the packet and have the damage value for that player (u8 + u32) per damage entry
	- No more massive packet size overhead when dealing with json but a bit more work to serialize/deserialize
	- removed old json packets
	- Dungeons dont send update packet unless a change has been made
	- Packets should be about 20% of the size they were before!
		- Client hello packet is about 30 bytes
		- Server hello packet is about 20 bytes
		- Dungeon DPS packets are a minimum of 64 bytes and with 16 players might be around 800 bytes
	- Known issues:
		- Might be an issue with player color being incorrect
- Client update rate is now configurable!
- Added 'speedyBoiMode' config option (default: false)
	- When enabled it turns on the dungeon timer chat messages
- Supports the new 102025.a1 update
	- Elites are now treated as field bosses
	- The only hard coded search is now Slime Diva
- Less errors hopefully

## 1.0.6
- Show ui while player is dead

## 1.0.5
- Detect classes with null class icons and replace with `_ico_caution_lv`
	- Players using the broken classes can be added to the damage lists now

## 1.0.4
- Fix party dps not being calculated in dungeons.
- Load sprites instead of textures so sliced rendering works (middle scales while edges dont, it looks way better)

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
