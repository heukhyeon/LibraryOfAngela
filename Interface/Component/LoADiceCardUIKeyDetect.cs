using LibraryOfAngela;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class LoADiceCardUIKeyDetect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static Dictionary<BattleDiceCardUI, LoADiceCardUIKeyDetect> detectorCache = new Dictionary<BattleDiceCardUI, LoADiceCardUIKeyDetect>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="card"></param>
    /// <param name="code"></param>
    /// <param name="onKeyPress">반환값이 false라면 일회성으로 판단하고 이 컴포넌트를 제거합니다.</param>
    public static void Create(BattleDiceCardUI ui, BattleUnitModel owner, BattleDiceCardModel card, KeyCode code, OnLoACardKeyPressListener onKeyPress)
    {
        LoADiceCardUIKeyDetect com;
        if (detectorCache.ContainsKey(ui))
        {
            com = detectorCache[ui];
        }
        else
        {
            com = ui.gameObject.AddComponent<LoADiceCardUIKeyDetect>();
            detectorCache[ui] = com;
        }

        com.Init(ui, card, owner, new List<KeyCode> { code }, onKeyPress);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="card"></param>
    /// <param name="code"></param>
    /// <param name="onKeyPress">반환값이 false라면 일회성으로 판단하고 이 컴포넌트를 제거합니다.</param>
    public static void Create(BattleDiceCardUI ui, BattleUnitModel owner, BattleDiceCardModel card, List<KeyCode> codes, OnLoACardKeyPressListener onKeyPress)
    {
        LoADiceCardUIKeyDetect com;
        if (detectorCache.ContainsKey(ui))
        {
            com = detectorCache[ui];
        }
        else
        {
            com = ui.gameObject.AddComponent<LoADiceCardUIKeyDetect>();
            detectorCache[ui] = com;
        }

        com.Init(ui, card, owner, codes, onKeyPress);
    }

    BattleDiceCardUI ui;
    BattleDiceCardModel card;
    BattleUnitModel owner;
    OnLoACardKeyPressListener onKeyPress;
    KeyCode[] codes;
    int keyCount;

    bool isEnter = false;

    public void Init(BattleDiceCardUI ui, BattleDiceCardModel card, BattleUnitModel owner, List<KeyCode> codes, OnLoACardKeyPressListener onKeyPress)
    {
        this.ui = ui;
        this.card = card;
        this.codes = codes.ToArray();
        keyCount = codes.Count;
        this.owner = owner;
        this.onKeyPress = onKeyPress;
        enabled = true;
    }

    void Update()
    {
        if (ui.CardModel != card)
        {
            enabled = false;
        }
        if (!isEnter) return;
        for (int i = 0; i < keyCount; i++)
        {
            var c = codes[i];
            if (Input.GetKey(c))
            {
                var controller = ServiceLocator.Instance.GetInstance<ILoARoot>().GetCardListControllerByCard(card, owner);
                if (!onKeyPress(ui, owner, card, controller, c))
                {
                    Destroy(this);
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isEnter = true;
    }

    // Token: 0x060028BA RID: 10426 RVA: 0x0010DA2C File Offset: 0x0010BC2C
    public void OnPointerExit(PointerEventData eventData)
    {
        isEnter = false;
    }

    void OnDestroy()
    {
        detectorCache.Remove(ui);
    }

}

public delegate bool OnLoACardKeyPressListener(BattleDiceCardUI ui, BattleUnitModel owner, BattleDiceCardModel card, ILoACardListController controller, KeyCode code);