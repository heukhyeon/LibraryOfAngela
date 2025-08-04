using BattleCharacterProfile;
using LibraryOfAngela.Battle;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.Buf
{
    internal class ShieldControllerImpl : ShieldController, BufControllerImpl
    {
        class BarrierHistoryData
        {
            public BattleCardBehaviourResult res;
            public BattleUnitModel target;
            public int previous;
            public int value;
            public int max;
        }

        private Dictionary<BattleUnitModel, BarrierComponent> components = new Dictionary<BattleUnitModel, BarrierComponent>();
        private List<BarrierHistoryData> reservedDataList = new List<BarrierHistoryData>();

        private bool isCheckRequire = false;

        public static ShieldControllerImpl _instance;

        public static ShieldControllerImpl Instance
        {
            get
            {
                if (_instance == null) _instance = new ShieldControllerImpl();
                return _instance;
            }
        }

        public string keywordId => null;

        public string keywordIconId => null;

        public void AddAdditionalKeywordDesc()
        {
            
        }

        public string GetBufActivatedText()
        {
            return "";
        }

        public string GetBufActivatedText(BattleUnitBuf_loaShield buf, string current)
        {
            return string.Format("공격 주사위 피격시 그 피해량을 {0} 감소시키고, 감소시킨 피해량만큼 이 효과가 감소한다.", buf.stack);
        }

        public string GetBufName()
        {
            return "역장";
        }

        public string GetKeywordText()
        {
            return "";
        }

        public void OnCreate(BattleUnitBuf_loaShield buf)
        {
            var owner = buf._owner;
            
            foreach (var effect in BattleInterfaceCache.Of<IHandleTakeShield>(buf._owner))
            {
                try
                {
                    effect.OnCreateShield(buf);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public void OnAddBuf(BattleUnitBuf_loaShield buf, int addedStack)
        {
            if (addedStack > 0)
            {
                foreach (var effect in BattleInterfaceCache.Of<IHandleTakeShield>(buf._owner))
                {
                    try
                    {
                        effect.OnAddShield(buf, addedStack);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }

            OnValueChanged(buf, buf.stack - addedStack);
        }

        public void OnRoundEnd(BattleUnitBuf_loaShield buf)
        {
            bool callDestroy = true;
            var listeners = BattleInterfaceCache.Of<IHandleTakeShield>(buf._owner).ToList();
            foreach (var effect in listeners)
            {
                try
                {
                    effect.OnRoundEndInShield(buf, ref callDestroy);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            if (callDestroy) {
                var reason = new LoAShieldDestroyReason.RoundEnd(buf.stack);
                buf.Destroy();
                foreach (var effect in listeners) 
                {
                    try
                    {
                        effect.OnDestroyShield(buf, reason);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }

        public void OnDestroy(BattleUnitBuf_loaShield buf)
        {
            int s = buf.stack;
            buf.stack = 0;
            OnValueChanged(buf, s);
        }

        public void OnDestroyManually(BattleUnitBuf_loaShield buf, BattleUnitModel attacker) {
            var request = new LoAShieldDestroyReason.Etc(attacker);
            foreach (var effect in BattleInterfaceCache.Of<IHandleTakeShield>(buf._owner))
            {
                try
                {
                    effect.OnDestroyShield(buf, request);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public void OnHandleBreakDamage(BattleUnitBuf_loaShield buf, int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf keyword)
        {
            if (resultDmg <= 0 || buf.IsDestroyed()) return;
            int previous = buf.stack;
            int firstDmg = resultDmg;
            int reduceStack = resultDmg > previous ? previous : resultDmg;
            if (type != DamageType.Attack) reduceStack = 0;
            int next = resultDmg - reduceStack;
            var listeners = BattleInterfaceCache.Of<IHandleTakeShield>(buf._owner).ToList();
            foreach (var effect in listeners)
            {
                try
                {
                    effect.BeforeHandleBreakDamageInShield(buf, resultDmg, type, attacker, keyword, ref next, ref reduceStack);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            bool isReduced = resultDmg != next;
            resultDmg = next;
            if (isReduced)
            {
                foreach (var effect in listeners)
                {
                    try
                    {
                        effect.AfterHandleBreakDamageInShield(buf, firstDmg, type, attacker, keyword, next);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            if (reduceStack > 0)
            {
                switch (type)
                {
                    case DamageType.Attack:
                        buf.ReduceStack(new LoAShieldReduceRequest.AttackBreakDamage(attacker.currentDiceAction?.currentBehavior, reduceStack));
                        break;
                    default:
                        buf.ReduceStack(new LoAShieldReduceRequest.AbilityBreakDamage(attacker, reduceStack, type, keyword));
                        break;
                }

                if (buf.stack <= 0) 
                {
                    var reason = new LoAShieldDestroyReason.StackZero(attacker);
                    buf.Destroy();
                    foreach (var effect in listeners) 
                    {
                        try
                        {
                            effect.OnDestroyShield(buf, reason);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e);
                        }
                    }
                }
            }

        }

        public void OnHandleDamage(BattleUnitBuf_loaShield buf, int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf keyword)
        {
            if (resultDmg <= 0 || buf.IsDestroyed()) return;
            int previous = buf.stack;
            int firstDmg = resultDmg;
            int reduceStack = resultDmg > previous ? previous : resultDmg;
            if (type != DamageType.Attack) reduceStack = 0;
            int next = resultDmg - reduceStack;
            var listeners = BattleInterfaceCache.Of<IHandleTakeShield>(buf._owner).ToList();
            foreach (var effect in listeners)
            {
                try
                {
                    effect.BeforeHandleDamageInShield(buf, resultDmg, type, attacker, keyword, ref next, ref reduceStack);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            bool isReduced = resultDmg != next;
            resultDmg = next;
            if (isReduced)
            {
                foreach (var effect in listeners)
                {
                    try
                    {
                        effect.AfterHandleDamageInShield(buf, firstDmg, type, attacker, keyword, next);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            if (reduceStack > 0)
            {
                switch (type)
                {
                    case DamageType.Attack:
                        buf.ReduceStack(new LoAKeywordBufReduceRequest.Attack(attacker.currentDiceAction?.currentBehavior, reduceStack));
                        break;
                    default:
                        buf.ReduceStack(new LoAShieldReduceRequest.AbilityDamage(attacker, reduceStack, type, keyword));
                        break;
                }

                if (buf.stack <= 0) 
                {
                    var reason = new LoAShieldDestroyReason.StackZero(attacker);
                    buf.Destroy();
                    foreach (var effect in listeners) 
                    {
                        try
                        {
                            effect.OnDestroyShield(buf, reason);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e);
                        }
                    }
                }
            }
        }

        public void OnValueChanged(BattleUnitBuf_loaShield buf, int previous)
        {
            if (StageController.Instance.IsLogState())
            {
                var res = buf._owner.battleCardResultLog.CurbehaviourResult;
                var target = reservedDataList.Find(d => d.res == res);
                if (target == null)
                {
                    target = new BarrierHistoryData { res = res, previous = previous };
                    reservedDataList.Add(target);
                }
                target.value = buf.stack;
                target.max = buf._owner.MaxHp;
                isCheckRequire = true;
            }
            else
            {
                GetComponent(buf._owner)?.UpdateValue(previous, buf.stack, buf._owner.MaxHp);
            }
        }

        public void OnUpdateCharacterProfile(BattleCharacterProfileUI instance)
        {
            if (!isCheckRequire) return;
            var unit = instance.UnitModel;
            BattleCardBehaviourResult res = null;
            if (RencounterManager.Instance._librarian == unit.view)
            {
                res = RencounterManager.Instance._currentLibrarianBehaviourResult;
            }
            if (RencounterManager.Instance._enemy == unit.view)
            {
                res = RencounterManager.Instance._currentEnemyBehaviourResult;
            }
            if (res != null)
            {
                var idx = reservedDataList.FindIndex(d => d.res == res);
                if (idx < 0) return;
                var data = reservedDataList[idx];
                GetComponent(unit)?.UpdateValue(data.previous, data.value, data.max);
                reservedDataList.RemoveAt(idx);
            }
            else
            {
                var idx = reservedDataList.FindIndex(d => d.target == unit);
                if (idx < 0) return;
                var data = reservedDataList[idx];
                GetComponent(unit)?.UpdateValue(data.previous, data.value, data.max);
                reservedDataList.RemoveAt(idx);
            }

            isCheckRequire = reservedDataList.Count > 0;
        }

        public void OnEndRencounter()
        {
            foreach (var data in reservedDataList)
            {
                GetComponent(data.target)?.UpdateValue(data.previous, data.value, data.max);
            }
            reservedDataList.Clear();
            isCheckRequire = false;
        }

        public void OnReduceStack(BattleUnitBuf_loaShield buf, LoAKeywordBufReduceRequest request)
        {
            var listeners = BattleInterfaceCache.Of<IHandleTakeShield>(buf._owner).ToList();
            int value = request.Stack;
            foreach (var effect in listeners)
            {
                try
                {
                    effect.BeforeTakeShieldReduce(buf, request, ref value);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            request.Stack -= value;
            buf.stack -= value;
            buf.OnAddBuf(-value);
            foreach (var effect in listeners)
            {
                try
                {
                    effect.AfterTakeShieldReduce(buf, request, value);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            if (buf.stack <= 0)
            {
                buf.Destroy();
                foreach (var effect in listeners)
                {
                    try
                    {
                        effect.OnDestroyShield(buf, new LoAShieldDestroyReason.StackZero(request.Attacker));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        }

        private BarrierComponent GetComponent(BattleUnitModel owner) {
           var com = components.SafeGet(owner);
           if (com?.IsDestroyed != false) {
                var target = SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.allyarray.FirstOrDefault(d => d.UnitModel == owner);
                if (target == null)
                {
                    target = SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.enemyarray.FirstOrDefault(d => d.UnitModel == owner);
                }
                if (target != null)
                {
                    com = target.GetComponent<BarrierComponent>() ?? target.gameObject.AddComponent<BarrierComponent>();
                }
                if (com != null) {
                    components[owner] = com;
                }
           }
           return com;
        }
    }

    class BarrierComponent : MonoBehaviour
    {
        public BattleCharacterProfileUI ui;
        public Text text;
        //private Image[] bars;
        private Image RootBar;

        public bool IsDestroyed { get; private set; } = false;

/*        private Image RootBar => bars[0];
        private Image DamagedBar => bars[1];

        private Image HealBar => bars[2];

        private Image ChildBar => bars[3];*/

        private float max;


        void Awake()
        {
            ui = GetComponent<BattleCharacterProfileUI>();
            //bars = BufAssetLoader.LoadObject("LoA_Barrier_Bar", ui.hpBar.img.transform.parent, -1f).GetComponentsInChildren<Image>();
            //RootBar.transform.localPosition = new Vector3(-243.9508f, -13.90003f, 0f);
            RootBar = Instantiate(ui.hpBar.img, ui.hpBar.img.transform.parent).GetComponent<Image>();
            RootBar.transform.localPosition = new Vector3(0f, -0.0001688004f, 0f);
            RootBar.color = new Color(0.207f, 0.439f, 1.0f);

            /*            var c = DamagedBar.color;
                        c.a = 0f;
                        DamagedBar.color = c;

                        c = HealBar.color;
                        c.a = 0f;
                        HealBar.color = c;

                        c = ChildBar.color;
                        c.a = 0f;
                        ChildBar.color = c;*/

            var com2 = BufAssetLoader.LoadObject("LoA_Barrier_Text", ui.uiRoot, -1f);
            com2.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            com2.transform.localPosition = new Vector3(290f, 100f, 0f);
            text = com2.GetComponentInChildren<Text>(true);
            text.color = new Color(0.207f, 0.439f, 1.0f);
        }

        Coroutine latestCoroutine;

        public void UpdateValue(int previous, int next, int max)
        {
            try 
            {
                this.max = max;
                if (!enabled)
                {
                    enabled = true;
                }
                if (latestCoroutine != null) StopCoroutine(latestCoroutine);
                latestCoroutine = StartCoroutine(UpdateHpBar(previous, next));
            }
            catch (Exception e) 
            {
                Logger.LogError(e);
            }

        }

        IEnumerator UpdateHpBar(float previous, float value)
        {
            float t = value / max;
            float x = Mathf.Lerp(-550f, 0f, t);
            Vector3 dstPos = RootBar.transform.localPosition;
            dstPos.x = x;
            float e = 0f;
            Vector3 srcPos = RootBar.transform.localPosition;
            srcPos.x = Mathf.Lerp(-550f, 0f, previous / max);
            // Debug.Log($"밸류 업데이트 : {previous} // {value}");
            while (e < 1f)
            {
                e += Time.deltaTime;
                RootBar.transform.localPosition = Vector3.Lerp(srcPos, dstPos, e);
                text.text = ((int) Mathf.Lerp(previous, value, e)).ToString();
                yield return YieldCache.waitFrame;
            }
            if (value <= 0f)
            {
                yield return new WaitForSeconds(1f);
                enabled = false;
            }
            yield break;
        }

        void OnDestroy() {
           IsDestroyed = true;
           Destroy(text.gameObject);
           Destroy(text.gameObject);
        }

        void OnEnable()
        {
            if (text != null && RootBar != null)
            {
                text.gameObject.SetActive(true);
                RootBar.gameObject.SetActive(true);
            }
        }

        void OnDisable()
        {
            if (text != null && RootBar != null)
            {
                text.gameObject.SetActive(false);
                RootBar.gameObject.SetActive(false);
            }
            //bars = null;
            Destroy(this);
        }
    }
}
