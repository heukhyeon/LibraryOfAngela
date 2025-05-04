// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    [ExecuteInEditMode]
    class LoALayerDetector : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> targets = new List<GameObject>();

        void Awake()
        {
            if (Application.isEditor)
            {
                targets = GetComponentsInChildren<ParticleSystem>().Select(d => d.gameObject).ToList();
            }
        }



        public void UpdateLayer(int layer)
        {
            foreach (var t in targets)
            {
                t.layer = layer;
            }
        }
    }
}
