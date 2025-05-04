using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoALoader
{
    class LoaderLogger
    {
        private static StringBuilder logger = new StringBuilder();

        public static void AppendLine()
        {
            logger.AppendLine();
        }

        public static void AppendLine(string s)
        {
            logger.AppendLine(s);
        }

        public static void Flush()
        {
            Debug.Log(logger.ToString());
            logger = null;
        }
    }
}
