﻿using System.Collections.Concurrent;
using EvidentInstruction.Config.Extension;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using EvidentInstruction.Helpers;
using System;
using System.Text;
using Newtonsoft.Json;
using EvidentInstruction.Models;
using EvidentInstruction.Models.Interfaces;
using EvidentInstruction.Config.Exceptions;

namespace EvidentInstruction.Config.Helpers
{
    public static class ConfigHelper 
    {       
        private static IFile file = new TextFile();

        /// <summary>
        /// Получение словаря dictionaryTags и списка повторных параметров dublicatesTagsList и проверить dublicatesTagsList
        /// </summary>
        public static ConcurrentDictionary<string, object> GetTagsDictionary(Models.Config config) 
        {
            var (tags, dublicatesTags) = AddParameters(config);
            
                if (dublicatesTags.Any())
                {
                    Log.Logger.Warning($"Json has {dublicatesTags.Count} dublicates:" + System.Environment.NewLine + $"{Message.CreateMessage(dublicatesTags)}");                    
                    throw new ConfigException($"Json has {dublicatesTags.Count} dublicates:" + System.Environment.NewLine + $"{Message.CreateMessage(dublicatesTags)}");
                }
                else
                {
                    Log.Logger.Information($"Dublicates not found");
                }
                if (tags.Any())
                {
                    Log.Logger.Information($"Dictionary created. Dictionary has {tags.Count} parameters");
                }
                else
                {
                    Log.Logger.Warning("Dictionary not created");
                }            

            return tags;
        }
        /// <summary>
        /// Получить словарь с тегами
        /// </summary>        
        public static ConcurrentDictionary<string, object> GetDictionary(string filename, string path)
        {            
            var content = file.GetContent(filename, path);

            if(string.IsNullOrWhiteSpace(content)) 
            {
                Log.Logger.Warning($"File \"{filename}\" is empty");
                return null;
            }       

            var config = DeserializeHelper.DeserializeObject<Models.Config>(content);
            //тут будет ошибка NUllReferenseExeption
            if(config==null) //не та проверка
            {
                Log.Logger.Warning("Json model is empty");
                return null;
            }

            return GetTagsDictionary(config);
        }

        /// <summary>
        /// Добавление в словарь DictionaryTags параметров ("key":"value") и дублей в dublicatesTagsList
        /// </summary>        
        public static (ConcurrentDictionary<string, object>, List<string>) AddParameters(Models.Config config) 
        {
            var tags = new ConcurrentDictionary<string, object>();
            var dublicatesTags = new List<string>();
            
             if (config.Parameters == null)
             {
                 Log.Logger.Information($"Keys \"config \" or \"parameters\" not found in Json ");
                 return (tags, dublicatesTags);
             } 

            foreach (var parameter in config.Parameters)
            {    
                foreach (var param in parameter.Param)
                {      
                    if (tags.ContainsKey(param.Key))
                        dublicatesTags.Add(param.Key);
                    else
                        tags.TryAdd(param.Key, param.Value);
                }
            }       

            return (tags, dublicatesTags);
        }
    }
}