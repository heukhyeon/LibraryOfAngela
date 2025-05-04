using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    public class LoACustomizedAppearance : SpecialCustomizedAppearance
    {
		public override void RefreshAppearanceByMotion(CharacterMotion motion)
		{
			if (this.list == null || this.list.Count <= 0)
			{
				return;
			}
			ActionDetail a = motion.actionDetail;
			ActionDetail a2 = a;
			SpecialCustomHead s = this.list.Find((SpecialCustomHead x) => x.detail == a);
			if (s == null)
			{
				s = this.list.Find((SpecialCustomHead x) => x.motionDirection == motion.motionDirection);
				if (s == null)
				{
					s = this.list[UnityEngine.Random.Range(0, this.list.Count)];
				}
			}
			else if (s.motionDirection != motion.motionDirection)
			{
				if (s.replaceHead != null)
				{
					s = this.list.Find((SpecialCustomHead x) => x.rootObject.name == s.replaceHead.name);
				}
				else
				{
					s = this.list.Find((SpecialCustomHead x) => x.motionDirection == motion.motionDirection);
					if (s == null)
					{
						s = this.list[UnityEngine.Random.Range(0, this.list.Count)];
					}
				}
			}
			if (s == null)
			{
				return;
			}
			if (s != null)
			{
				int num = 1000;
				int num2 = 1000;
				int num3 = 1000;
				int num4 = 1000;
				SpriteSet spriteSet = null;
				for (int i = 0; i < motion.motionSpriteSet.Count; i++)
				{
					int sortingOrder = motion.motionSpriteSet[i].sprRenderer.sortingOrder;
					switch (motion.motionSpriteSet[i].sprType)
					{
						case CharacterAppearanceType.FrontHair:
							if (sortingOrder < num2)
							{
								num2 = sortingOrder;
							}
							break;
						case CharacterAppearanceType.RearHair:
							if (sortingOrder < num3)
							{
								num3 = sortingOrder;
							}
							break;
						case CharacterAppearanceType.Face:
							if (motion.motionSpriteSet[i].sprRenderer.gameObject.name != "blr" && sortingOrder < num)
							{
								num = sortingOrder;
							}
							break;
						case CharacterAppearanceType.Head:
							if (sortingOrder < num4)
							{
								num4 = sortingOrder;
							}
							spriteSet = motion.motionSpriteSet[i];
							break;
					}
				}
				if (num3 == 1000)
				{
					num3 = -2;
				}
				if (num >= 1000)
				{
					num = num4 + 10;
				}
				if (num2 >= 1000)
				{
					num2 = num + 10;
				}
				this.SetSortingOrder(s.headRenderer, num4);
				this.SetSortingOrder(s.faceRenderer, num);
				if (s.additionalFace != null)
				{
					for (int j = 0; j < s.additionalFace.Count; j++)
					{
						this.SetSortingOrder(s.additionalFace[j], num);
					}
				}
				this.SetSortingOrder(s.frontHairRenderer, num2);
				if (s.additionalFrontHair != null)
				{
					for (int k = 0; k < s.additionalFrontHair.Count; k++)
					{
						this.SetSortingOrder(s.additionalFrontHair[k], num2);
					}
				}
				this.SetSortingOrder(s.rearHairRenderer, num3);
				if (s.additionalRearHair != null)
				{
					for (int l = 0; l < s.additionalRearHair.Count; l++)
					{
						this.SetSortingOrder(s.additionalRearHair[l], num3);
					}
				}
				if (this._currentHead == null)
				{
					this._currentHead = s;
				}
				else
				{
					this._currentHead.rootObject.SetActive(false);
				}
				if (spriteSet != null)
				{
					if (motion.customPivot == null)
					{
						base.transform.parent = spriteSet.sprRenderer.transform;
					}
					else
					{
						base.transform.parent = motion.customPivot;
					}
				}
				else
				{
					Debug.Log("There is not [Head] in motion." + motion.actionDetail);
				}
				base.transform.localPosition = Vector3.zero;
				base.transform.localRotation = Quaternion.identity;
				base.transform.localScale = Vector3.one * this._scaleFactor;
				s.rootObject.SetActive(true);
				this._currentHead = s;
				this._currentHead.rootObject.transform.localPosition = Vector3.zero;
				this._currentHead.rootObject.transform.localRotation = Quaternion.identity;
			}
			CustomizedAppearance.OnRefreshAppearance onRefreshAppearance = this._onRefreshAppearance;
			if (onRefreshAppearance == null)
			{
				return;
			}
			onRefreshAppearance(this);
		}
	}
}
