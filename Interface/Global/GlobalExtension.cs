// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryOfAngela;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;

public static class GlobalExtension
{
    public static EmotionCardXmlInfo FindMyEmotionCard(this ILoACustomEmotionMod owner, int id)
    {
        return ServiceLocator.Instance.GetInstance<ILoAEmotionDictionary>().FindEmotionCard(owner.packageId, id);
    }
}
