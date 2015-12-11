using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server
{
        public enum GPICommand : byte
        {
            ShowCrawl = 0xA0,
            HideCrawl = 0xAA,
            AspectNarrow = 0xA1,
            AspectWide = 0xA2,
            ShowLogo0 = 0xB0, // do 0xB9
            HideLogo = 0xBA,
            ShowParental0 = 0x20, // do 0x29
            HideParental = 0x2A,
            MasterTake = 0xC0, // make this client a master and override others
            MasterFree = 0xC7,
            SetCrawl = 0xAB, // 2nd byte crawl number
            ReloadCrawl = 0xAD, // 2nd byte crawl number
            AuxShow = 0xD0,
            AuxHide = 0xDA,
            Color = 0xE0,
            Mono = 0xE1,
            HeartBeat = 0xA8,
            GetInfo = 0x70,
            SetIsController = 0x11,
            PlayoutStart = 0xFA,

        }
}
