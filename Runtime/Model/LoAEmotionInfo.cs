using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public class LoAEmotionInfo : EmotionCardXmlInfo
    {
        public string packageId = "";

        public LoAEmotionInfo() : base()
        {

        }

        public LoAEmotionInfo(string packageId, EmotionCardXmlInfo origin) : this()
        {
            this.packageId = packageId;
            this.id = origin.id;
            this.Name = origin.Name;
            this._artwork = origin._artwork;
            this.State = origin.State;
            this.TargetType = origin.TargetType;
            this.Level = origin.Level;
            this.EmotionLevel = origin.EmotionLevel;
            this.EmotionRate = origin.EmotionRate;
            this.Locked = origin.Locked;
            this.Sephirah = origin.Sephirah;
            this.Script = origin.Script;
        }
    }
}
