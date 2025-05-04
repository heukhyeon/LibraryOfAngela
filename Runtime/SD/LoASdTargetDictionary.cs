using LibraryOfAngela.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workshop;

namespace LibraryOfAngela.SD
{
    class LoASdTargetDictionary : Singleton<LoASdTargetDictionary>
    {
        private Dictionary<CharacterSound, LoASDTarget> dicBySound = new Dictionary<CharacterSound, LoASDTarget>();
        private Dictionary<CharacterAppearance, LoASDTarget> dicByApperance = new Dictionary<CharacterAppearance, LoASDTarget>();
        private Dictionary<WorkshopSkinDataSetter, LoASDTarget> dicBySetter = new Dictionary<WorkshopSkinDataSetter, LoASDTarget>();
        public void Add(LoASDTarget target)
        {
            dicBySound[target.appearance.soundInfo] = target;
            dicByApperance[target.appearance] = target;
            dicBySetter[target.setter] = target;
        }
        public void Clear()
        {
            foreach (var c in dicBySetter)
            {
                c.Value.Dispose();
            }
            dicBySound.Clear();
            dicByApperance.Clear();
            dicBySetter.Clear();
        }

        public LoASDTarget this[CharacterSound sound] => dicBySound.SafeGet(sound);
        public LoASDTarget this[CharacterAppearance appearance] => dicByApperance.SafeGet(appearance);
        public LoASDTarget this[WorkshopSkinDataSetter setter] => dicBySetter.SafeGet(setter);

    }
}
