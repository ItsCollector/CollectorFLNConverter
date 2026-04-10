# Collector FLN Converter
A tool for Osu!mania to convert charts to full long note

<img width="471" height="612" alt="image" src="https://github.com/user-attachments/assets/90a24ded-8cfe-44f0-b3e2-7505d3523c74" />

## Features
- Auto-detects current map via memory reading
- Easy to use GUI 
- Converts maps to FLN format
- Automatically imports into Osu!
- Configurable gap, OD, HP with the option to override the map's values
- Remove SVs
- Settings save for next time you open the program
  
## Setup
- Download the latest release here: https://github.com/ItsCollector/CollectorFLNConverter/releases/tag/v1.1.2
- Unzip the x86 folder
- Launch CollectorFLN.exe, with Osu! open. (recommend creating a shortcut for this application and putting it on your desktop)
- If your Osu! install is not in the default local user location, click the "Link Osu! Song Folder" button then and select the Osu! songs folder directory
- If you don't see a "Link Osu! Folder" button then the program has already linked it automatically
- Enjoy converting maps

## Notes
- Only supports osu! (stable), not osu!lazer
- Requires osu! to be running
- Works for Windows only

## To-do List
- Native linux support
- Option to use snap-based LN gaps 
- Found an interesting edge case with the help of Cassio (FLN God, so ET btw) where if the file name is too long, it can crash Osu! lmao. I should make a tweak to make the file name shorter if the artist name / song name makes it too long
- Some old BMS files with hit sounds don't import because either the hit object line doesn't have the corresponding hit sound OR i could remove the samples section worst case scenario.
- Occasionally there is a false flag format issue when converting hit objects for unknown reason, preventing certain charts from converting (this might be fixed with the locale patch in v1.1.1+, monitoring for any reports).
- Wrong scroll speed because the wrong BPM was found to be the dominant one in some rare case.
  
## Stars 
- If my program helped you, leave a star on Github!

## Special Thanks
https://github.com/Piotrekol/ProcessMemoryDataFinder - For providing memory processing tools for Osu!
