﻿using System;

namespace Molder.Web.Infrastructures
{
    public static class CommandSetting
    {
        public static int RETRY = 5;
        public static TimeSpan INTERVAL = TimeSpan.FromMilliseconds(25);
    }
}
