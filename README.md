# Open Playout Automation
Open Playout Automation is television broadcast solution. It is in development in Polish national television broadcaster - TVP (Telewizja Polska) and works as main MCR play-out system in its regional channels. Project uses SVT's [CasparCG] as clip player. It also intensively uses [FFmpeg] as media information and transcoding tool. 
It can be used as a simple (even multi-) channel-in-a-box solution, as well as in much more sophisticated environment (e.g. 2 players and control workstation with additional stuff, as GPI controlled devices).

## Main features 
### Playout
- multiple channels
- multi-user with rights management based on PC network address
- redundant output for every channel
- GPI as start source, aspect ratio control and external graphics trigger
- partial clip playout (via media trimming or logical sub-clips)

### Rundowns
- variety of rundown event types: movie, live, graphics, Flash CG templates
- rundown nesting (aka blocks)
- event nesting (e.g. movie may contain graphics)
- event-based GPI triggering to control: branding, parental control and information crawl
- frame-accurate time calculations

### Ingest
- from watchfolders
- from ftp's
- linear (tape or live) ingest with deck control using [CasparCG fork](https://github.com/jaskie/Server)
- from Sony [XDCAM](http://en.wikipedia.org/wiki/XDCAM) decks, supports metadata and subclip ingest
- extensive clip playout-related metadata

### Archiving
- "shallow" archive for temporarily unnecessary media
- preserving clip metadata

## System requirements
They mainly follow [CasparCG] requirements.
- Windows as base operating system
- .NET 4.5 as application platform
- Blackmagic Design [Decklink] as inputs and outputs cards

## Instalation
Refer to [wiki] pages.

## Contact
Don't hesitate.

[CasparCG]: http://www.casparcg.com
[FFmpeg]: http://ffmpeg.org/
[Decklink]: https://www.blackmagicdesign.com/products/decklink
[wiki]: https://github.com/jaskie/PlayoutAutomation/wiki
