using LOR_XML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Interface_Internal
{

    internal interface ILoAArtworkGetter
    {
        bool ContainsKey(string packageId, string name);
        Sprite GetSprite(string packageId, string name, bool showWarning);
    }

    internal interface ILoAEmotionDictionary
    {
        List<EmotionCardXmlInfo> GetEmotionCardListByMod(ILoACustomEmotionMod mod, Predicate<EmotionCardXmlInfo> condition);

        List<AbnormalityCard> GetEmotionCardDescListByMod(ILoACustomEmotionMod mod);

        EmotionCardXmlInfo FindEmotionCard(string packageId, int id);

        bool IsModCard(ILoACustomEmotionMod mod, EmotionCardXmlInfo target);
    }

    internal interface IPatcher
    {
        void Patch(Type targetType, string name, string patchName, Type[] paramTypes, PatchScope scope);

        void Patch(MethodBase target, MethodInfo prefixMethod = null, MethodInfo postfixMethod = null, MethodInfo finalizeMethod = null, PatchScope scope = PatchScope.FORERVER);

        void PatchTranspiler(Type targetType, Type patchType, string methodName, string patchName, Type[] paramTypes = null);

        void Unpatch(Type targetType, string methodName, PatchType type);

        void PatchAll(Type targetType);
    }

}
