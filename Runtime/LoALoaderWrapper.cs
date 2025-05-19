using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    class LoALoaderWrapper
    {
        public static Assembly loaderAssemlby;

        public static Task WaitCallInitializerComplete()
        {
            return loaderAssemlby.GetType("LoALoader.FileParser").GetMethod("WaitCallInitializerComplete", AccessTools.all)
                .Invoke(null, null) as Task;
        }

        public static Task WaitCardWorkComplete()
        {
            return loaderAssemlby.GetType("LoALoader.FileParser").GetMethod("WaitCardWorkComplete", AccessTools.all)
    .Invoke(null, null) as Task;
        }

        public static Task WaitDataComplete()
        {
            return loaderAssemlby.GetType("LoALoader.FileParser").GetMethod("WaitDataComplete", AccessTools.all)
    .Invoke(null, null) as Task;
        }
        
        public static MethodBase GetAddFileData()
        {
            return loaderAssemlby
                .GetType("LoALoader.FileLoader")
                .GetMethod("LoadData", AccessTools.all, null, new Type[] { typeof(string), typeof(string) }, null);
        }

    }
}
