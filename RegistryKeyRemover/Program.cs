using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace RegistryKeyRemover
{
    class Program
    {
        private static readonly List<string> Phrases = new List<string>();
        private static readonly List<string> RemovedKeys = new List<string>();

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");

            if (args.Length < 1)
            {
                Console.WriteLine("You need to specify a search phrase as an argument!");
                Console.ReadKey();
                return;
            }
            Phrases.AddRange(args);

            Console.WriteLine("Starting looking up keys in registry... ");

            CheckAndRemoveKey(Registry.ClassesRoot);
            CheckAndRemoveKey(Registry.CurrentConfig);
            CheckAndRemoveKey(Registry.CurrentUser);
            CheckAndRemoveKey(Registry.LocalMachine);
            CheckAndRemoveKey(Registry.Users);

            Console.WriteLine("Finished dealing with registry.");
            Console.WriteLine("Removed keys count = " + RemovedKeys.Count);
            Console.WriteLine("Removed keys = " + string.Join(", ", RemovedKeys.ToArray()));
            Console.ReadKey();
        }

        private static void CheckAndRemoveKey(RegistryKey registryKey)
        {
            if(registryKey == null)
                return;

            Console.WriteLine("Checking RegistryKey: " + registryKey.Name);
            if(registryKey.SubKeyCount > 0)
            {
                string[] subKeyNames = new string[0];
                try
                {
                    subKeyNames = registryKey.GetSubKeyNames();
                }
                catch (IOException exception)
                {
                    Console.WriteLine(exception);
                }
                foreach (string subKeyName in subKeyNames)
                {
                    try
                    {
                        RegistryKey key = registryKey.OpenSubKey(subKeyName);
                        CheckAndRemoveKey(key);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            if(registryKey.ValueCount > 0)
            {
                string[] valueNames = registryKey.GetValueNames();
                foreach (string valueName in valueNames)
                {
                    object value = registryKey.GetValue(valueName);
                    if (value is string valueAsString)
                    {
                        if (ContainsPhrase(valueAsString))
                        {
                            Console.WriteLine("Removing value: " + valueName);
                            RemovedKeys.Add(valueName);
                            registryKey.DeleteValue(valueName);
                        }
                    }
                }
            }
        }

        private static bool ContainsPhrase(string value)
        {
            foreach (string phrase in Phrases)
            {
                if (value.ToLower().Contains(phrase.ToLower()))
                {
                    return true;
                }       
            }
            return false;
        }
    }
}
