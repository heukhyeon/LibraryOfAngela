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
        private static int cnt = 0;
        private static long startTime = 0L;
        private static long time = 0L;

        public static void Log(string s, bool withBlank = false)
        {
            if (Enabled || LoAFramework.DEBUG)
            {
                Debug.Log("LoA ::" + s);
            }
            else
            {
                cnt++;
                time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (withBlank) currentLogger.AppendLine();
                currentLogger.AppendLine("LoA :: " + s);
                if (withBlank) currentLogger.AppendLine();
            }
        }

        public static void CheckFlush(float modLoadingProgress)
        {
            var current = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (cnt == 0)
            {
                var percent = (modLoadingProgress * 100f);
                var diff = (current - startTime) / 1000.0;
                Debug.Log($"LoA :: No Update, Maybe CallInitializer Complete Wait, Current Progress : {percent}%, Duration : {diff}s");
            }
            else if (current - time >= 5000L)
            {
                var log = "LoA :: Flash Log, Count :" + cnt + "\n";
                log += currentLogger.ToString();
                log += "\n\n";
                currentLogger = new StringBuilder();
                cnt = 0;
                time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Debug.Log(log);
            }
            else
            {
                var percent = (modLoadingProgress * 100f);
                var diff = (current - startTime) / 1000.0;
                Debug.Log($"LoA :: Updated Current, So Wait, Current Progress : {percent}%, Duration : {diff}s");
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
            startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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
