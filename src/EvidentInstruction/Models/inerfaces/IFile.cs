﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EvidentInstruction.Models
{
    public interface IFile
    {

        string Filename { get; set; }
        string Path { get; set; }
        string Content { get; set; }
        string Url { get; set; }
        bool Create(string filename, string path, string content = null);
        bool Delete(string filename, string path);
        bool DownloadFile(string url, string filename, string pathToSave);
        string Get();
        bool IsExist(string filename, string path);

    }
}
