// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public abstract class LoACustomFaceData : UnitCustomizingData
    {
        public string packageId;

        public virtual string PrefabKey => null;

        public virtual bool IsDestroyOriginalFace(string currentSkinName)
        {
            return true;
        }

        public virtual string GetFrontFaceArtwork(ActionDetail action)
        {
            return null;
        }

        public virtual string GetRearFaceArtwork(ActionDetail action)
        {
            return null;
        }

        public abstract string GetSettingFaceArtwork();
    }
}
