using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Model
{
    public class LoASkinComponent : MonoBehaviour
    {
        protected CharacterAppearance Appearance { get; private set; }
        // BattleUnitModel 의 참조가 필요한경우 true 로 지정합니다.
        protected virtual bool IsRequireOwnerReference { get => false; }
        protected ActionDetail CurrentMotion { get => Appearance._currentMotion.actionDetail; }
        protected BattleUnitModel owner { get; private set; }
        protected bool IsCharacterView
        {
            get
            {
                var parent = transform?.parent;
                if (parent is null) return false;
                if (parent.gameObject.name != "CharactersRoot") return true;
                var myIndex = transform.GetSiblingIndex();
                var parentLast = parent.childCount - 1;
                return myIndex >= 5 && myIndex == parentLast;
            }
        }

        [Obsolete("Preview 때는 언제나 만들어지지 않습니다")]
        protected bool IsPreview
        {
            get
            {
                var parent = transform?.parent;
                if (parent is null) return true;
                return parent.gameObject.name == "CharactersRoot";
            }
        }

        public virtual void Initialize(CharacterAppearance appearance)
        {
            Appearance = appearance;
            appearance.AddOnCharMotionChanged(OnCharMotionChanged);
            if (IsRequireOwnerReference && !IsPreview)
            {
                owner = appearance.GetComponentInParent<BattleUnitView>()?.model;
            }
        }

        protected virtual void Start()
        {
            // 복제된 경우 없을 수 있음
            if (Appearance is null)
            {
                Initialize(GetComponent<CharacterAppearance>());
            }
        }

        protected virtual void OnCharMotionChanged()
        {

        }

        protected virtual void OnDestroy()
        {
            Appearance?.RemoveOnCharMotionChanged(OnCharMotionChanged);
        }

        public virtual void OnLayerChanged(string layerName)
        {

        }

        public virtual ActionDetail ConvertMotion(ActionDetail motion)
        {
            return motion;
        }

        public virtual bool IsMoveable(LoAMoveType moveType)
        {
            return true;
        }
    }
}
