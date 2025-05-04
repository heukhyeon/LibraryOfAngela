// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

namespace LibraryOfAngela.Story
{
    class LoATimelineObserver : MonoBehaviour
    {
        public static Dictionary<UIStoryProgressPanel, LoATimelineObserver> Instances = new Dictionary<UIStoryProgressPanel, LoATimelineObserver>();
        public UIStoryProgressPanel panel;
        public bool isKeyDetectEnabled = false;
        public KeyCode[] targetKeyCodes;
        public Action<KeyCode> onPress;

        void OnDisable()
        {
            TimelineConflict.Instance.ViewPortDisabled(panel);
        }

        void Update()
        {
            if (isKeyDetectEnabled)
            {
                foreach (var c in targetKeyCodes)
                {
                    if (Input.GetKeyDown(c))
                    {
                        onPress(c);
                    }
                }
            }

        }
    }
}
