﻿using Molder.Helpers;
using Microsoft.Extensions.Logging;
using System;

namespace Molder.Models.Directory
{
    public class BaseDirectory : DefaultDirectory
    {
        public override string Get()
        {
            Log.Logger().LogInformation("Work with BaseDirectory");
            return AppContext.BaseDirectory;
        }
    }
}
