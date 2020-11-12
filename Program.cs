using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Homework
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Program started");
            ReadDataFiles();
            LogConfigs();
            Console.ReadKey();
        }

        //Set value of config
        private static void SetValueRecursively<T>(string sectionPathKey, dynamic jsonObj, T value)
         {
            string filePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\appSettings.json";
            var remainingSections = sectionPathKey.Split(":", 2);   //split the string at the ':' characters

            var currentSection = remainingSections[0];
            try
            {
                if (remainingSections.Length > 1)
                {
                    var nextSection = remainingSections[1];
                    SetValueRecursively(nextSection, jsonObj[currentSection], value);
                }
                else
                {
                    jsonObj[currentSection] = value;    //end of the tree where we set the value
                }
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, output);
            }
            catch(Exception e)
            {
                Console.WriteLine("This config {0} was not in the datatype.", sectionPathKey);
            }
        }

        //Divide file to smaller parts and make actions
        static void ReadDataFiles()
        {
            string filePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Data\Project_Config.txt";
            string lineOne = File.ReadAllText(filePath);
            lineOne = ReplaceSybols(lineOne);
            var sections = lineOne.Split("Section");

            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i].StartsWith(':'))
                {
                    int begin = sections[i].IndexOf(':');
                    int end = sections[i].IndexOf('=');
                    string one = sections[i].Substring(begin + 2, (end - begin) - 3) + ":";
                    sections[i] = sections[i].Remove(begin + 2, (end - begin) - 3);
                    string reduceMultiSpace = @"[ ]{2,}";
                    sections[i] = Regex.Replace(sections[i].Replace(":  ===", ""), reduceMultiSpace, " ");
                    var subSections = sections[i].Split("-");

                    for (int j = 1; j < subSections.Length; j++)
                    {
                        subSections[j] = subSections[j].TrimStart(' ');
                        int beginS = 0;
                        int endS = subSections[j].IndexOf(':');
                        string two = subSections[j].Substring(beginS, (endS - beginS));
                        subSections[j] = subSections[j].Remove(beginS , (endS - beginS));
                        string result = one + two + ":";

                        subSections[j] = Regex.Replace(subSections[j].Replace("\r\n", "|"), reduceMultiSpace, " ");
                        subSections[j] = subSections[j].TrimStart(':', '|');
                        subSections[j] = Regex.Replace(subSections[j].Replace("|===", ""), reduceMultiSpace, " ");
                        
                        var configs = subSections[j].Split("|");
                        if(configs.Length>1)
                        {
                            if (configs[1].Length > 1)
                            {
                                for (int z = 0; z < configs.Length; z++)
                                {
                                    int endConfig = configs[z].IndexOf(':');
                                    string config = configs[z].Substring(0, endConfig);
                                    configs[z] = configs[z].Remove(0, endConfig + 1);
                                    config = result + config;
                                    var value = configs[z];
                                    ValidateConfig(config, value);
                                }
                            }
                            else
                            {
                                int endConfig = configs[0].IndexOf(':');
                                string config = configs[0].Substring(0, endConfig);
                                configs[0] = configs[0].Remove(0, endConfig + 1);
                                result = result + config;
                                var value = configs[0];
                                ValidateConfig(result, value);
                            }
                        }
                        else
                        {
                            int endConfig = configs[0].IndexOf(':');
                            string config = configs[0].Substring(0, endConfig);
                            configs[0] = configs[0].Remove(0, endConfig + 1);
                            result = result + config;
                            var value = configs[0];
                            ValidateConfig(result, value);
                        }
                    }
                }
            }
        }

        //Method for tabs, empty lines and line comments deletion
        static string ReplaceSybols(string text)
        {
            var lineComments = @"//(.*?)\r?\n";

            text = Regex.Replace(text, lineComments, me => {
                if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                    return me.Value.StartsWith("//") ? Environment.NewLine : "";
                return me.Value;
            },
    RegexOptions.Singleline);                                                                                   //removed line comments

            const string reduceMultiSpace = @"[ ]{2,}";
            var line = Regex.Replace(text.Replace("\t", ""), reduceMultiSpace, " ");                            //removed tabs
            var resultString = Regex.Replace(line, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);      //removed empty lines

            return resultString;
        }

        //Method to get jsonObj
        static void AddConfig<T>(string config, T value)
        {
            string filePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\appSettings.json";
            string json = File.ReadAllText(filePath);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            SetValueRecursively(config, jsonObj, value);
        }

        //Method for configs value validation
        static void ValidateConfig(string config, string value)
        {
            var sections = config.Split(":", 3);

            switch (sections[2])
            {
                case "ordersPerHour":
                    try
                    {
                        int result = Int32.Parse(value);
                        AddConfig(config, result);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Value is invalid '{value}' in config '{config}'");
                    }
                    break;
                case "orderLinesPerOrder":
                    try
                    {
                        int result = Int32.Parse(value);
                        AddConfig(config, result);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Value is invalid '{value}' in config '{config}'");
                    }
                    break;
                case "inboundStrategy":
                    if(value == "random" || value == "optimized")
                        Console.WriteLine($"Value is invalid '{value}' in config '{config}'");
                    else 
                        AddConfig(config, value);
                    break;
                case "powerSupply":
                    if (value == "normal" || value == "big")
                        Console.WriteLine($"Value is invalid '{value}' in config '{config}'");
                    else
                        AddConfig(config, value);
                    break;
                case "resultStartTime":
                    try
                    {
                        DateTime dateTime = DateTime.ParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture);
                        AddConfig(config, dateTime.ToString("hh:mm:ss"));
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Value is invalid '{value}' in config '{config}'");
                    }
                    break;
                case "resultInterval":
                    try
                    {
                        int result = Int32.Parse(value);
                        AddConfig(config, result);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Value is invalid '{value}' in config '{config}'");
                    }
                    break;
                default:
                    AddConfig(config, value);
                    break;

            }
        }
        

        //Method for logging appSetings.json file
        static void LogConfigs()
        {
            string filePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\appSettings.json";
            var configuration = new ConfigurationBuilder()
               .AddJsonFile(filePath, false, true)
               .Build();

            var ordersPerHour = configuration.GetValue<int>("Data Generation:Order Profile:ordersPerHour");
            var orderLinesPerOrder = configuration.GetSection("Data Generation:Order Profile:orderLinesPerOrder").Value;
            var powerSupply = configuration.GetSection("System Config:Power Supply:powerSupply").Value;
            Console.WriteLine(ordersPerHour);
            Console.WriteLine(orderLinesPerOrder);
            Console.WriteLine(powerSupply);
        }
    }
}
