using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    public interface ILoAArtworkData
    {
        string packageId { get; set; }

        string path { get; }
    }

    public interface ILoAArtworkCache
    {
        Sprite this[string name] { get; }

        Sprite GetNullable(string name);

    }
}
