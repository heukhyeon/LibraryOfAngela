using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace LibraryOfAngela.CorePage
{
    class RairtyTextComponent : MonoBehaviour
    {
        private TextMeshProUGUI textCom;
        private bool init = false;
        private Color32 latestNormalColor;
        private Color32 latestApplyColor;

        public void UpdateTarget(BookModel target)
        {
            if (!init)
            {
                init = true;
                textCom = GetComponent<TextMeshProUGUI>();
                latestNormalColor = textCom.color;
            }
            if (target != null && AdvancedCorePageRarityPatch.corePageInnerLines.ContainsKey(target.BookId))
            {
                latestApplyColor = AdvancedCorePageRarityPatch.corePageInnerLines[target.BookId];
                textCom.color = latestApplyColor;
            } 
            else if (textCom.color == latestApplyColor)
            {
                textCom.color = latestNormalColor;
            }
        }
    }
}
