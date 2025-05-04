// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoALoader
{
    class FileSearcher
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("version.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool VerQueryValue(IntPtr pBlock, string lpSubBlock, out IntPtr lplpBuffer, out uint puLen);

        [DllImport("version.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetFileVersionInfoSize(string lptstrFilename, out uint lpdwHandle);

        [DllImport("version.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetFileVersionInfo(string lptstrFilename, uint dwHandle, uint dwLen, byte[] lpData);

        [StructLayout(LayoutKind.Sequential)]
        private struct VS_FIXEDFILEINFO
        {
            public uint dwSignature;
            public uint dwStrucVersion;
            public uint dwFileVersionMS;
            public uint dwFileVersionLS;
            public uint dwProductVersionMS;
            public uint dwProductVersionLS;
            public uint dwFileFlagsMask;
            public uint dwFileFlags;
            public uint dwFileOS;
            public uint dwFileType;
            public uint dwFileSubtype;
            public uint dwFileDateMS;
            public uint dwFileDateLS;
        }

        public static (uint, uint, uint, uint) GetDllVersion(string filePath)
        {
            uint handle;
            uint size = GetFileVersionInfoSize(filePath, out handle);

            if (size == 0)
            {
                throw new InvalidOperationException("파일 버전 정보를 가져올 수 없습니다.");
            }

            byte[] buffer = new byte[size];
            if (!GetFileVersionInfo(filePath, handle, size, buffer))
            {
                throw new InvalidOperationException("파일 버전 정보를 읽는 데 실패했습니다.");
            }

            if (!VerQueryValue(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), @"\",
                out IntPtr versionInfoPtr, out uint versionInfoLen))
            {
                throw new InvalidOperationException("버전 정보를 파싱할 수 없습니다.");
            }

            var versionInfo = Marshal.PtrToStructure<VS_FIXEDFILEINFO>(versionInfoPtr);

            return (
                (versionInfo.dwFileVersionMS >> 16),
                (versionInfo.dwFileVersionMS & 0xFFFF),
                (versionInfo.dwFileVersionLS >> 16),
                (versionInfo.dwFileVersionLS & 0xFFFF)
            );
        }
    }
}
