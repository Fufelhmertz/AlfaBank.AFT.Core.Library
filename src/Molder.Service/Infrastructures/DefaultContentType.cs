﻿using System.Diagnostics.CodeAnalysis;

namespace Molder.Service.Infrastructures
{
    [ExcludeFromCodeCoverage]
    public static class DefaultContentType
    {
        public static string JSON = "application/json";
        public static string TEXT = "text/plain";
        public static string XML = "text/xml";
    }
}
