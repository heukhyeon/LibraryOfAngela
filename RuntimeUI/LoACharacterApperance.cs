using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    public class LoACharacterApperance : CharacterAppearance
    {
        [SerializeField]
        private List<LoALayerDetector> layerDetectors = new List<LoALayerDetector>();


        public void UpdateLayer(string layerName)
        {
            if (layerDetectors is null) return;

            int layer = LayerMask.NameToLayer(layerName);
            foreach (var d in layerDetectors)
            {
                d.UpdateLayer(layer);
            }
        }
    }
}
