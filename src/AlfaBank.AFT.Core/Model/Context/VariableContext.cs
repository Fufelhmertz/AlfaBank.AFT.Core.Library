﻿using AlfaBank.AFT.Core.Helpers;
using AlfaBank.AFT.Core.Model.KeyValues;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit.Sdk;

namespace AlfaBank.AFT.Core.Model.Context
{
    public class VariableContext
    {
        private const string XmlPattern = "{([^}]*)}";
        private const string JsonPattern = @"@(.\w+)";
        public KeyValues<Variable> Variables { get; set; } = new KeyValues<Variable>();

        public string GetVariableName(string key)
        {
            var varName = key;
            if(varName.IndexOf('.') > 0)
            {
                varName = varName.Substring(0, varName.IndexOf('.'));
            }

            if(varName.IndexOf('[') > 0)
            {
                varName = varName.Substring(0, varName.IndexOf('['));
            }

            return varName;
        }

        public Variable GetVariable(string key)
        {
            return Variables.SingleOrDefault(_ => _.Key == GetVariableName(key)).Value;
        }

        public void SetVariable(string key, Type type, object value)
        {
            var varName = GetVariableName(key);
            var vars = Variables;
            vars[varName] = new Variable() { Type = type, Value = value };
            Variables = vars;
            Console.WriteLine($"[DEBUG] IN VARIABLE \"{key}\" RECORDED THE VALUE \"{value}\"");
        }

        public object GetVariableValue(string key)
        {
            var name = key;
            var index = -1;
            string path = null;
            if(key.IndexOf('.') > 0)
            {
                name = key.Substring(0, key.IndexOf('.'));
                path = key.Substring(key.IndexOf('.') + 1);
            }

            if(name.IndexOf('[') > 0 && name.IndexOf(']') > name.IndexOf('['))
            {
                if(!int.TryParse(key.Substring(name.IndexOf('[') + 1, Math.Max(0, name.IndexOf(']') - name.IndexOf('[') - 1)), out index))
                {
                    index = -1;
                }

                name = key.Split('[').First();
            }

            var var = Variables.SingleOrDefault(_ => _.Key == name).Value;
            if(var == null)
            {
                return null;
            }

            var varValue = var.Value;
            var varType = var.Type;

            if(varValue == null)
            {
                return varType.GetDefault();
            }

            if(varType.HasElementType && index >= 0)
            {
                varValue = ((object[])varValue)[index];
                varType = varType.GetElementType();
            }

            if(typeof(JObject).IsAssignableFrom(varType))
            {
                var jsonObject = JObject.Parse((string)varValue);
                return jsonObject.SelectToken(path?.Remove(0, 2) ?? "/*") ?? null;
            }

            if(typeof(JToken).IsAssignableFrom(varType))
            {
                var jsonToken = JToken.Parse((string)varValue);
                return jsonToken.SelectToken(path?.Remove(0, 2) ?? "/*") ?? null;
            }

            if(typeof(XNode).IsAssignableFrom(varType))
            {
                return ((XNode)varValue).XPathSelectElement(path ?? "/*");
            }

            if(typeof(XmlNode).IsAssignableFrom(varType))
            {
                return ((XmlNode)varValue).SelectSingleNode(path ?? "/*");
            }

            if(!typeof(DataTable).IsAssignableFrom(varType))
            {
                return varValue;
            }

            if(!int.TryParse(key.Substring(key.IndexOf('[') + 1, key.IndexOf(']') - key.IndexOf('[') - 1), out index))
            {
                index = -1;
            }

            var row = ((DataTable)varValue).Rows[index];

            var offset = key.IndexOf(']') + 1;
            if(key.IndexOf('[', offset) < 0)
            {
                return row[
                    key.Substring(
                        key.IndexOf('[', offset) + 1,
                        key.IndexOf(']', offset) - key.IndexOf('[', offset) - 1)];
            }

            return int.TryParse(key.Substring(key.IndexOf('[', offset) + 1, key.IndexOf(']', offset) - key.IndexOf('[', offset) - 1), out index) ? row[index] : row[key.Substring(key.IndexOf('[', offset) + 1, key.IndexOf(']', offset) - key.IndexOf('[', offset) - 1)];
        }

        public string GetVariableValueText(string key)
        {
            var val = GetVariableValue(key);

            if (val == null)
            {
                return null;
            }

            string ret;
            switch(val)
            {
                case XElement element when element.HasElements == false:
                    ret = element.Value;
                    break;

                case XmlNode node when node.FirstChild.GetType() == typeof(XmlText):
                {
                    ret = node.FirstChild.Value;
                    break;
                }

                case JToken token when token.HasValues == true:
                {
                    ret = token.Root.ToString();
                    break;
                }

                default:
                    ret = Reflection.ConvertObject<string>(val);
                    break;
            }

            return ret;
        }

        public string ReplaceVariablesInXmlBody(string str, Func<object, string> foundReplace = null, Func<string, string> notFoundReplace = null)
        {
            return ReplaceVariables(str, XmlPattern, foundReplace, notFoundReplace);
        }

        public string ReplaceVariablesInJsonBody(string str, Func<object, string> foundReplace = null, Func<string, string> notFoundReplace = null)
        {
            return ReplaceVariables(str, JsonPattern, foundReplace, notFoundReplace);
        }

        private string ReplaceVariables(string str, string pattern, Func<object, string> foundReplace = null, Func<string, string> notFoundReplace = null)
        {
            object val;
            var fmt = Regex.Replace(
                str ?? string.Empty,
                pattern,
                m =>
                {
                    if(m.Groups[1].Value.Length <= 0 || m.Groups[1].Value[0] == '(')
                    {
                        return $"{m.Groups[1].Value}";
                    }

                    if(GetVariable(m.Groups[1].Value) == null)
                    {
                        return notFoundReplace != null ? notFoundReplace(m.Groups[1].Value) : m.Groups[1].Value;
                    }

                    val = GetVariableValue(m.Groups[1].Value);
                    return foundReplace != null ? foundReplace(val) : Reflection.ConvertObject<string>(val);
                },
                RegexOptions.None);
            return fmt;
        }
    }
}