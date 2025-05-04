using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    class Logger
    {
        private static StringBuilder currentLogger;

        public static bool Enabled { get; set; } = false;

        public static void Log(string s, bool withBlank = false)
        {
            if (Enabled || LoAFramework.DEBUG)
            {
                Debug.Log("LoA ::" + s);
            }
            else
            {
                if (withBlank) currentLogger.AppendLine();
                currentLogger.AppendLine("LoA :: " + s);
                if (withBlank) currentLogger.AppendLine();
            }
        }

        public static void LogError(Exception e)
        {
            if (Enabled)
            {
                Debug.LogError(e);
            }
            else
            {
                currentLogger.AppendLine(e.ToString());
            }
        }

        public static void Open()
        {
            Enabled = false;
            currentLogger = new StringBuilder();
            if (LoAFramework.DEBUG)
            {
                Debug.Log("LoA :: Current Mod Is Debug");
            }
        }

        public static void Flush()
        {
            Enabled = true;
            if (!LoAFramework.DEBUG)
            {
                Debug.Log(currentLogger.ToString());
            }
        
        }

    }
}
