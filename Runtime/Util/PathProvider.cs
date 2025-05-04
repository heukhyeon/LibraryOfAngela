using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Util
{
    internal static class PathProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packageId">대상 모드 패키지 ID, 이 값을 통해 해당 모드의 원본 모드 폴더 경로를 획득합니다.</param>
        /// <param name="path">얻고자 하는 경로, <see cref="ModContentManager.Instance.GetModPath"/>/Assemblies 로 시작하는경우 절대경로로 인식하며, 그 외의 경우 저 값을 앞에 추가해서 반환합니다.</param>
        /// <returns></returns>
        public static string ConvertValidPath(string packageId, string path)
        {
            var targetModPath = Path.Combine(ModContentManager.Instance.GetModPath(packageId), "Assemblies");
            if (path.StartsWith(targetModPath)) return path;
            return Path.Combine(targetModPath, path);
        }
    }
}
