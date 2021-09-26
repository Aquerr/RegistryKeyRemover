using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using System.Threading.Tasks;

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

            Task classsesRootTask = Task.Run(() => CheckAndRemoveKey(Registry.ClassesRoot));
            Task currentConfigTask = Task.Run(() => CheckAndRemoveKey(Registry.CurrentConfig));
            Task currentUserTask = Task.Run(() => CheckAndRemoveKey(Registry.CurrentUser));
            Task localMachineTask = Task.Run(() => CheckAndRemoveKey(Registry.LocalMachine));
            Task usersTask = Task.Run(() => CheckAndRemoveKey(Registry.Users));

            Task task = Task.WhenAll(new Task[]{ classsesRootTask, currentConfigTask, currentUserTask, localMachineTask, usersTask }).ContinueWith((task1) => PrintCompletion());
            task.Wait();
        }

        private static void PrintCompletion()
        {
            Console.WriteLine("Finished dealing with registry.");
            Console.WriteLine("Removed keys count = " + RemovedKeys.Count);
            Console.WriteLine("Removed keys = " + string.Join(", ", RemovedKeys.ToArray()));
            Console.ReadKey();
        }

        private static bool CheckAndRemoveKey(RegistryKey registryKey)
        {
            bool hasSubKeys = true;
            bool hasValues = true;
            bool shouldRemoveKey = false;

            if(registryKey == null)
                return shouldRemoveKey;

            if(registryKey.SubKeyCount > 0)
            {
                int subKeyCount = registryKey.SubKeyCount;
                int removedSubKeys = 0;
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
                        if(subKeyName.Equals("UserSettings"))
                        {
                            Console.WriteLine("BOOM");
                        }

                        RegistryKey key = registryKey.OpenSubKey(subKeyName, true);
                        bool shouldRemoveSubKey = CheckAndRemoveKey(key);
                        if(shouldRemoveSubKey)
                        {
                            registryKey.DeleteSubKey(subKeyName);
                            removedSubKeys++;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Can't open key: " + subKeyName);
                    }
                }

                if (subKeyCount == removedSubKeys)
                    hasSubKeys = false;
            }

            if(registryKey.ValueCount > 0)
            {
                int keyValuesCount = registryKey.ValueCount;
                int removedValuesForKey = 0;
                string[] valueNames = registryKey.GetValueNames();
                foreach (string valueName in valueNames)
                {
                    if(ContainsPhrase(valueName))
                    {
                        Console.WriteLine("Removing valuename: " + valueName);
                        RemovedKeys.Add(valueName);
                        registryKey.DeleteValue(valueName);
                        removedValuesForKey++;
                        //continue;
                    }

                    //object value = registryKey.GetValue(valueName);
                    //if (value is string valueAsString)
                    //{
                    //    if (ContainsPhrase(valueAsString))
                    //    {
                    //        Console.WriteLine("Removing value: " + valueAsString);
                    //        RemovedKeys.Add(valueAsString);
                    //        registryKey.DeleteValue(valueName);
                    //        removedValuesForKey++;
                    //    }
                    //}
                }

                if (keyValuesCount == removedValuesForKey)
                    hasValues = false;
            }

            if (!hasValues && !hasSubKeys)
                shouldRemoveKey = true;

            return shouldRemoveKey;
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
