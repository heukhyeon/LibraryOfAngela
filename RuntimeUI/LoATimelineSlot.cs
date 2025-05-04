// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LibraryOfAngela
{
    public class LoATimelineSlot : MonoBehaviour
    {
        public Image icon;
        public Image iconGlow;
        public TMPro.TextMeshProUGUI text;
        public string timeline;
        [SerializeField]
        private LoATimelineButton parent;
		private bool isOn = false;

		private void Awake()
        {
			if (!(text is null))
            {
				text.fontMaterial = new Material(text.fontSharedMaterial);
			}
        }

		public void OnPointerEnter(BaseEventData eventData)
		{
			parent?.onEnterSlot?.Invoke();
			this.SetHighlight(true);
		}

		// Token: 0x06006C7A RID: 27770 RVA: 0x0023CF90 File Offset: 0x0023B190
		public void OnPointerExit(BaseEventData eventData)
		{
			this.SetHighlight(false);
		}

		private void OnDisable()
        {
			SetHighlight(false);
        }


		public void Click()
        {
            parent.ClickTimeline(this);
        }

		private void SetHighlight(bool enabled)
        {
			if (isOn == enabled) return;
			isOn = enabled;
            var color = enabled ? parent.highlightColor : parent.normalColor;
			text.color = color;
			iconGlow.color = color;
			InitMaterialProperty(color);
        }

		public void InitMaterialProperty(Color underlayColor)
		{
			/*			this.tm.fontMaterial.SetColor("_FaceColor", Color.white);
						this.tm.fontMaterial.SetFloat("_FaceDilate", 0f);
						this.tm.fontMaterial.SetFloat("_OutlineSoftness",0f);
						this.tm.fontMaterial.SetColor("_OutlineColor", Color.black);
						this.tm.fontMaterial.SetFloat("_OutlineWidth", 0f);*/
			/*			if (this.underlayOn)
						{
							this.tm.fontMaterial.EnableKeyword("UNDERLAY_ON");
						}
						else
						{
							this.tm.fontMaterial.DisableKeyword("UNDERLAY_ON");
						}*/
			var material = text.fontMaterial;
			// if (text.fontMaterial.HasProperty("_UnderlayColor"))
			// {
				material.SetColor("_UnderlayColor", underlayColor);
/*				material.SetFloat("_UnderlayOffsetX", 0f);
				material.SetFloat("_UnderlayOffsetY", 0f);
				material.SetFloat("_UnderlayDilate", 0.2f);
				material.SetFloat("_UnderlaySoftness", 0.8f);*/
			// }
/*			if (this.glowOn)
			{
				this.tm.fontMaterial.EnableKeyword("GLOW_ON");
			}
			else
			{
				this.tm.fontMaterial.DisableKeyword("GLOW_ON");
			}
			if (this.tm.fontMaterial.HasProperty("_GlowColor"))
			{
				this.tm.fontMaterial.SetColor("_GlowColor", this.glowColor);
				this.tm.fontMaterial.SetFloat("_GlowOffset", this.glowOffset);
				this.tm.fontMaterial.SetFloat("_GlowInner", this.glowInner);
				this.tm.fontMaterial.SetFloat("_GlowOuter", this.glowOuter);
				this.tm.fontMaterial.SetFloat("_GlowPower", this.glowPower);
			}*/
		}
	}
}
