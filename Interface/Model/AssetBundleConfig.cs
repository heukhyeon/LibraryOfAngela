using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public abstract class AssetBundleConfig : LoAConfig
    {
        public abstract List<AssetBundleInfo> GetAssetBundleInfos();
    }
}
