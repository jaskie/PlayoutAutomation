﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IArchiveDirectoryConfig
    {
        ulong idArchive { get; set; }
        string Folder { get; set; }
    }
}