using LibraryOfAngela.CorePage;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Workshop;

namespace LibraryOfAngela.SD
{
    class LoASDTarget
    {
        public static Dictionary<SkinComponentKey, List<RenderCacheData>> skinSet;
        public WorkshopSkinData targetData;
        public WorkshopSkinDataSetter setter;
        public CharacterAppearance appearance;
        public string skinName;
        public Dictionary<ActionDetail, RenderCacheData> dataDic = new Dictionary<ActionDetail, RenderCacheData>();
        private List<ActionDetail> motions = new List<ActionDetail>();

        public SkinComponentKey key;
     
        public LoASkinComponent component = null;

        public static LoASDTarget Create(WorkshopSkinDataSetter __instance, WorkshopSkinData data)
        {
            var packageId = data.contentFolderIdx;
            var skinName = data.dataName;
            if (data is LoAWorkshopImportSkinData d)
            {
                packageId = d.originPackageId;
                skinName = d.originSkinName;
            }
            var key = new SkinComponentKey { packageId = packageId, skinName = skinName };
            if (!skinSet.ContainsKey(key))
            {
                if (LoAFramework.DEBUG)
                {
                    Logger.Log($"SdTarget Skip : {key}");
                }
                return null;
            }
            Logger.Log($"SdTarget Create : {key}");
            var target = new LoASDTarget();
            target.targetData = data;
            target.setter = __instance;
            target.appearance = __instance.Appearance;
            target.skinName = skinName;
            target.key = key;
            return target;
        }

        public void InitSMotionObj()
        {
            var origin = appearance._motionList[0];
            foreach (var action in targetData.dic.Keys)
            {
                if ((int)action >= 12 && !setter.Appearance._characterMotionDic.ContainsKey(action))
                {
                    var motion = UnityEngine.Object.Instantiate(origin, origin.transform.parent);
                    motion.actionDetail = action;
                    motion.gameObject.name = "LoA_AddtionalApperance_" + action;
                    appearance._motionList.Add(motion);
                    appearance._characterMotionDic.Add(action, motion);
                }
            }
        }

        public void InitSfxTargets(Dictionary<ActionDetail, string> sfxDic)
        {
            if (sfxDic is null) return;
            var sound = appearance.soundInfo;
            sound._motionSounds.RemoveAll(x => sfxDic.ContainsKey(MotionConverter.MotionToAction(x.motion)));
            foreach (var k in sfxDic.Keys) sound._dic.Remove(MotionConverter.ActionToMotion(k));
        }
    
        public void LateInit()
        {
            try
            {
                foreach (var match in skinSet[key])
                {
                    // 스킨은 instinate 때문에 좋든 싫든 한번은 다 UpdateMotion 거쳐야함.
                    if (match.motion == ActionDetail.Default && match.realSprite != null && !match.hasSkinSprite) continue;

                    motions.Add(match.motion);
                    dataDic[match.motion] = match;

                    if (match.motion == ActionDetail.Default)
                    {
                        UpdateMotion(match, setter.parts[ActionDetail.Standing]);
                        UpdateMotion(match, setter.parts[ActionDetail.Default]);
                    }
                }

                var com = AdvancedSkinInfoPatch.Instance.skinComponentTypes?.SafeGet(key);
                if (com != null)
                {
                    var component = (LoASkinComponent)setter.gameObject.AddComponent(com);
                    component.Initialize(appearance);
                    this.component = component;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"LateInitError ...? {key}");
                Logger.LogError(e);
            }

        }
    
        public void Dispose()
        {
            dataDic.Clear();
            motions.Clear();
            setter = null;
            targetData = null;
        }
    
        public ActionDetail ConvertMotion(ActionDetail detail)
        {
            var m = component?.ConvertMotion(detail);
            if (m != null && m.Value != detail)
            {
                return m.Value;
            }
            if (!motions.Contains(detail))
            {
                if (detail == ActionDetail.Fire || detail == ActionDetail.Aim)
                {
                    if (motions.Contains(ActionDetail.Penetrate))
                    {
                        return ActionDetail.Penetrate;
                    }
                }
                if (detail == ActionDetail.Slash || detail == ActionDetail.Penetrate || detail == ActionDetail.Hit)
                {
                    if (motions.Contains(ActionDetail.Fire))
                    {
                        return ActionDetail.Fire;
                    }
                }
                if (detail >= ActionDetail.Special)
                {
                    return motions.RandomOne(d => (d >= ActionDetail.Slash && d <= ActionDetail.Hit) || (d == ActionDetail.Fire));
                }
            }
            return detail;
        }

        public void ChangeMotion(ActionDetail detail)
        {
            var targetParts = this.setter.parts.SafeGet(detail);
            if (targetParts is null) return;
            var key = detail == ActionDetail.Standing ? ActionDetail.Default : detail;
            var targetData = this.dataDic.SafeGet(key);
            if (!(targetData is null))
            {
                UpdateMotion(targetData, targetParts);
            }
            else if (!(targetParts.front?.sprite is null))
            {
                var order = appearance?._customAppearance?.GetRendererOrder(CharacterAppearanceType.FrontHair, detail) ?? -1;
                if (order < 0) order = targetParts.rear.sortingOrder;
                targetParts.front.sortingOrder = order + 150;
            }

        }
    
        public void OnLayerChanged(string layerName)
        {
            component?.OnLayerChanged(layerName);
        }
    
        private void UpdateMotion(RenderCacheData targetData, WorkshopSkinDataSetter.PartRenderer renderer)
        {
            var realSprite = targetData.realSprite;
            Sprite frontRealSprite = targetData.frontRealSprite;
            bool hasFront = !(frontRealSprite is null);
            bool hasSkin = !(targetData.skinSprite is null);
            if (realSprite == null)
            {
                realSprite = LoASpriteLoader.LoadRealSprite(targetData, LoASpriteLoader.SkinType.Normal);
                frontRealSprite = LoASpriteLoader.LoadRealSprite(targetData, LoASpriteLoader.SkinType.Front);
                hasFront = !(frontRealSprite is null);
                if (!hasSkin && targetData.hasSkinSprite) {
                    targetData.skinSprite = LoASpriteLoader.LoadRealSprite(targetData, LoASpriteLoader.SkinType.Skin);
                    hasSkin = !(targetData.skinSprite is null); 
                    targetData.frontSkinSprite = LoASpriteLoader.LoadRealSprite(targetData, LoASpriteLoader.SkinType.Front | LoASpriteLoader.SkinType.Skin);
                    if (!hasSkin)
                    {
                        targetData.hasSkinSprite = false;
                    }
                }
            }
            if (hasFront)
            {
                targetData.frontRealSprite = frontRealSprite;
            }

            targetData.realSprite = realSprite;
            var originData = this.targetData.dic[targetData.motion];
            if (hasFront)
            {
                originData._frontSprite = frontRealSprite;
                originData._sprite = realSprite;
            }
            else if (originData.direction == CharacterMotion.MotionDirection.SideView)
            {
                originData._frontSprite = realSprite;
            }
            else
            {
                originData._sprite = realSprite;
            }
            renderer.rear.sprite = realSprite;
            if (hasFront)
            {
                renderer.front.sprite = targetData.frontRealSprite;
                renderer.front.gameObject.SetActive(true);
                var order = appearance?._customAppearance?.GetRendererOrder(CharacterAppearanceType.FrontHair, targetData.motion) ?? -1;
                if (order < 0) order = renderer.rear.sortingOrder;
                renderer.front.sortingOrder = order + 150;
            }
            if (hasSkin)
            {
                var skinRear = UnityEngine.Object.Instantiate(renderer.rear, renderer.rear.transform);
                skinRear.gameObject.name = "LoA_Skin_Sprite_Rear";
                skinRear.sprite = targetData.skinSprite;
                skinRear.sortingOrder = skinRear.sortingOrder + 1;
                appearance._motionList.Find(d => d.actionDetail == renderer.action)?.motionSpriteSet?.Add(new SpriteSet(skinRear, CharacterAppearanceType.Skin));
                if (!(targetData.frontSkinSprite is null))
                {
                    skinRear = UnityEngine.Object.Instantiate(renderer.front, renderer.front.transform);
                    skinRear.gameObject.name = "LoA_Skin_Sprite_Front";
                    skinRear.sprite = targetData.frontSkinSprite;
                    skinRear.sortingOrder = skinRear.sortingOrder + 1;
                    appearance._motionList.Find(d => d.actionDetail == renderer.action)?.motionSpriteSet?.Add(new SpriteSet(skinRear, CharacterAppearanceType.Skin));
                }
            }
            this.dataDic.Remove(targetData.motion);
        }
    }
}
