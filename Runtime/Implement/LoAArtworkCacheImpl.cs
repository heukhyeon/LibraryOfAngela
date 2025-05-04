using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Implement
{
    class LoAArtworkCache : ILoAArtworkCache
    {
        private string packageId;

        public LoAArtworkCache(string packageId)
        {
            this.packageId = packageId;
        }

        public Sprite this[string name] => LoAArtworks.Instance.GetSprite(packageId, name, true);

        public Sprite GetNullable(string name) => LoAArtworks.Instance.GetSprite(packageId, name, false);
    }
}
