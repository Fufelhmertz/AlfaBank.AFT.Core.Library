﻿using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Molder.Configuration.Tests.Extension
{
    [ExcludeFromCodeCoverage]
    public static class StringExtensions
    {
        public static Stream ToStream(this string s)
        {
            return s.ToStream(Encoding.UTF8);
        }

        public static Stream ToStream(this string s, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(s ?? ""));
        }
    }
}