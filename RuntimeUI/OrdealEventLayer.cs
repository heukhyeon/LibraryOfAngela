// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela
{
#pragma warning disable 0649
    public class OrdealEventLayer : MonoBehaviour
    {
        [SerializeField]
        private Text titleComponent;
        [SerializeField]
        private Text subTitleComponent;
        [SerializeField]
        private Text messageComponent;
        [SerializeField]
        private Image[] colorTargets;
        [SerializeField]
        private CanvasGroup group;

        public Canvas canvas;
        //private const float MAX_TIME = 4f;
        private const float FADE_IN = 0.5f;
        private const float DISPLAY = 3f;
        private const float FADE_OUT = 1f;
        private Status status = Status.NONE;
        private float elepsed = 0f;
        private enum Status
        {
            NONE,
            FADEIN,
            DISPLAY,
            FADEOUT
        }


        public void Show(string title, string subtitle, string message, Color color)
        {
            titleComponent.text = title;
            subTitleComponent.text = subtitle;
            messageComponent.text = message;
            foreach (var c in colorTargets)
            {
                c.color = color;
            }
            titleComponent.color = color;
            subTitleComponent.color = color;
            messageComponent.color = color;
            subTitleComponent.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            status = Status.FADEIN;
            elepsed = 0f;
            gameObject.SetActive(true);
        }

        public void FontUpdate(Font font)
        {
            titleComponent.font = font;
            subTitleComponent.font = font;
            messageComponent.font = font;
        }

        private void Update()
        {
            if (status == Status.FADEIN)
            {
                group.alpha = elepsed / FADE_IN;
            }
            else if (status == Status.FADEOUT)
            {
                group.alpha = 1f - (elepsed / FADE_OUT);
            }
            elepsed += Time.deltaTime;
            if (status == Status.FADEIN && elepsed > FADE_IN)
            {
                elepsed = 0f;
                status = Status.DISPLAY;
            }
            else if (status == Status.DISPLAY && elepsed >= DISPLAY)
            {
                elepsed = 0f;
                status = Status.FADEOUT;
            }
            else if (status == Status.FADEOUT && elepsed >= FADE_OUT)
            {
                elepsed = 0f;
                status = Status.NONE;
                gameObject.SetActive(false);
            }
        }

    }
#pragma warning restore 0649
}
