# Collector FLN Converter
A tool for Osu!mania to convert charts to full long note

<img width="459" height="644" alt="image" src="https://github.com/user-attachments/assets/cca8218a-4067-4329-bdb1-77d2c6cc27d7" />

## Features
- Auto-detects current map via memory reading
- Easy to use GUI 
- Converts maps to FLN format
- Automatically imports into Osu!
- Configurable gap, OD, HP with the option to override the map's values
- Remove SVs
- Settings save for next time you open the program
  
## Setup
- Download the latest release here: https://github.com/ItsCollector/CollectorFLNConverter/releases/tag/v1.2.1
- Unzip the folder
- Launch CollectorFLN.exe, with Osu! open. (recommend creating a shortcut for this application and putting it on your desktop)
- If your Osu! install is not in the default local user location, click the "Link Osu! Song Folder" button then and select the Osu! songs folder directory
- If you don't see a "Link Osu! Songs Folder" button then the program has already linked it automatically
- Enjoy converting maps

## Notes
- Only supports osu! (stable), not osu!lazer
- Requires osu! to be running
- Works for Windows only

## To-do List
- Built-in rate changer 
- Separation of SV removal and rate changer to create a 3 in 1 mania tool
- Native linux support
- Fix a rounding issue when converting using snap configured LN gap. You likely won't notice the issue in practice unless you open AI mod so it's not a major fault
  
## Stars 
- If my program helped you, leave a star on Github!

## Special Thanks
- Cassio - Due to their long term usage, they have identified broken maps for me to fix edge cases and mistakes in implementation
- Percyqaz - Helped me identify a fault with converting maps that end with LNs originally
- https://github.com/Piotrekol/ProcessMemoryDataFinder - For providing memory processing tools for Osu!
