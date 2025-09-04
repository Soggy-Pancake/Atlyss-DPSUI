# Atlyss DPSUI

This mod displays your DPS in Atlyss. Both the server and the client need to have the mod installed for it to work properly!
It uses [CodeTalker](https://thunderstore.io/c/atlyss/p/Robyn/CodeTalker/) to send the DPS data from the server to the client.

It does still work as a client only mod but it only tracks damage that you do yourself. Other player's damage can't be tracked.

## Requirements

- [CodeTalker](https://thunderstore.io/c/atlyss/p/Robyn/CodeTalker/)
- [EasySettings](https://thunderstore.io/c/atlyss/p/Nessie/EasySettings/) (Optional)

## Building

Make a copy of `Config.Build.user.props.template` and remove `.template`. Then set `GameDir` and `ProfileDir`.
- `GameDir` Points to the same folder as `ATLYSS.exe`
- `ProfileDir` Points to your thunderstore profile folder.

Then build.

## License

The license is GNU GPLv3.