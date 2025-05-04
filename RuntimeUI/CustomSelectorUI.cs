using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.BattleUI
{
#pragma warning disable 0649
    public class CustomSelectorUI : MonoBehaviour
    {
        public GameObject root;
        public TMPro.TMP_Text title;
        public ScrollRect scrollView;
        public Image artwork;

        [SerializeField]
        private Image background;
        [SerializeField]
        private float backgroundAlpha = 0f;
        [SerializeField]
        private float backgroundAlpha2 = 0f;
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private List<Image> colorTargets;
        [SerializeField]
        private Image bg;

        private float _lastAlpha = -100f;
        private int showState = 1;
        private float elepsed = 0f;

        void LateUpdate()
        {
            if (showState == 1) return;

            if (_lastAlpha != backgroundAlpha)
            {
                _lastAlpha = backgroundAlpha;
                background.material.SetFloat("_Dissolve_Value", backgroundAlpha);
            }
            if (backgroundAlpha2 != bg.color.a)
            {
                bg.color = new Color(bg.color.r, bg.color.b, bg.color.b, bg.color.a);
            }
            if (showState == -1)
            {
                elepsed += Time.deltaTime;
                if (elepsed > 2f)
                {
                    animator.enabled = false;
                    gameObject.SetActive(false);
                }
            }
            else if (showState == 0)
            {
                elepsed += Time.deltaTime;
                if (elepsed > 1f)
                {
                    elepsed = 0f;
                    showState = 1;
                    scrollView.content.gameObject.SetActive(true);
                    title.gameObject.SetActive(true);
                }
            }
        }

        public void Show(Color color)
        {
            animator.enabled = false;
            colorTargets.ForEach(d =>
            {
                d.color = color;
            });
            animator.enabled = true;
            gameObject.SetActive(true);
            animator.SetTrigger("On");
            showState = 0;
            elepsed = 0f;
        }

        public void JustShow()
        {
            if (showState != 1)
            {
                Show(Color.red);
            }
            else
            {
                Hide();
            }

        }

        public void Hide()
        {
            animator.SetTrigger("Off");
            showState = -1;
            elepsed = 0f;
        }


    }
#pragma warning restore 0649
}
