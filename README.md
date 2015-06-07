# Open Playout Automation
Open Playout Automation is television broadcast solution. It is in development in Polish national television broadcaster - TVP (Telewizja Polska) and intendent to become automation system for its regional channels. Currently works (as a proof-of concept, with partial functionality, however in daily broadcast) in three regional branches. Project widely profits from SVT's [CasparCG]. It also intensively uses [FFmpeg]. 
It can be used as a simple (even multi-) channel-in-a-box solution, as well as in much more sophisticated environment (e.g. 2 players and control workstation with additional stuff, as GPI controlled devices).

## Main features 
### Playout
- multiple channels
- redundant output for every channel
- GPI as start source, aspect ratio control and external graphics trigger

### Rundowns
- variety of rundown event types: movie, live, graphics
- rundown nesting (aka blocks)
- event nesting (e.g. movie may contain graphics)
- event-based GPI triggering to control: branding, parental control and information crawl
- frame-accurate time calculations

### Ingest
- from watchfolders
- from ftp's
- from Sony [XDCAM](http://en.wikipedia.org/wiki/XDCAM) decks, supports metadata and subclip ingest
- partial movie playout (via media trimming or logical sub-clips)
- extensive clip playout-related metadata

### Archiving
- "shallow" archive for unnecessary media
- preserving clip metadata

## System requirements
They mainly follow [CasparCG] requirements.
- Windows 7 x64 as stable operating system
- .NET 4.0 as application platform
- Blackmagic Design [Decklink] as inputs and outputs cards

## Instalation
Refer to [wiki] pages.

## Contact
Don't hesitate.

[CasparCG]: http://www.casparcg.com
[FFmpeg]: http://ffmpeg.org/
[Decklink]: https://www.blackmagicdesign.com/products/decklink
[wiki]: https://github.com/jaskie/PlayoutAutomation/wiki
