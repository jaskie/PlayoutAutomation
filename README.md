# Open Playout Automation
Open Playout Automation is television broadcast solution. It is in development in Polish national television broadcaster - [TVP](http://www.tvp.pl) (Telewizja Polska) and intendent to become automation system for its regional channels. Currently works (as a proof-of concept, with partial functionality, however in daily broadcast) in three regional branches. Project widely profits from SVT's [CasparCG]. It also intensively uses [FFmpeg]. 
It can be used as a simple (even multi-) channel-in-a-box solution, as well as in much more sophisticated environment (e.g. 2 players and control workstation with additional stuff, as GPI controlled devices).

## Main features 

### Architecture
- multiple channels in one system, multiple clients to each channel (in progress: limiting user's rights)
- full system redundancy - two simultaneous outputs from separate machines, using its own copy of media files
- preview from Newtek [NDI]&reg; sources (our [CasparCG fork] can be one) and bar audio level monitor
- easy to use user interface optimized for continous usage

### Rundowns
- variety of rundown event types: movie, live, graphics, parametrized animations
- rundown nesting (aka blocks) and grouping in containers
- event nesting (e.g. movie may contain graphics)
- event-based GPI triggering to control: branding, parental control and information crawl
- frame-accurate time calculations, clip trimming on various levels
- audio loudness ([EBU R128](https://tech.ebu.ch/docs/r/r128.pdf) compliant) measurment and volume adjustment
- cut/copy/paste support, rundown import-export

### Ingest
- from local watchfolders, remote shares and ftp's
- [NEW] linear (tape) ingest with or without deck control using our [CasparCG fork] and a [Decklink] card
- from Sony [XDCAM](http://en.wikipedia.org/wiki/XDCAM) decks, supports metadata and subclip ingest
- partial movie playout (via media trimming or logical sub-clips)
- extensive clip playout-related metadata

### Archiving
- "shallow" archive for unnecessary media
- preserving clip metadata

## System requirements
They mainly follow [CasparCG] requirements.
- Windows 7 x64 as stable operating system
- .NET 4.5 as application platform

## Instalation
Refer to [wiki] pages.

## Contact
Don't hesitate.

[CasparCG]: http://www.casparcg.com
[CasparCG fork]: https://github.com/jaskie/Server
[FFmpeg]: http://ffmpeg.org/
[Decklink]: https://www.blackmagicdesign.com/products/decklink
[NDI]: https://www.newtek.com/ndi/
[wiki]: https://github.com/jaskie/PlayoutAutomation/wiki
