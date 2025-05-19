using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoALoader
{
    public class EntryObserver : MonoBehaviour
    {
        public static void Create(EntryScene scene)
        {
            var observer = scene.gameObject.AddComponent<EntryObserver>();
            observer.scene = scene;
        }

        public EntryScene scene;
        private bool flag = false;
        void Update()
        {
            // 한국 연령 알림 나오면 강제로 꺼버림
            if (!flag && scene.ob_deliberationAlarmKr.activeSelf)
            {
                flag = true;
                scene.EndDeliberationAlarmInAnim();
            }
        }
    }
}
