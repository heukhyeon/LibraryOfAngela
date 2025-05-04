using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LibraryOfAngela.Extension.CommonExtension;

namespace LibraryOfAngela.Extension
{
    public static class PatchTextension
    {
        public static Type Patch(this Type owner, string name, params Type[] types)
        {
            ServiceLocator.Instance.CreateInstance<IPatcher>(GetCallerAssembly()).Patch(owner, name, null, types, scope: PatchScope.FORERVER);
            return owner;
        }

        public static Type Patch(this Type owner, string name, string patchName, params Type[] types)
        {
            ServiceLocator.Instance.CreateInstance<IPatcher>(GetCallerAssembly()).Patch(owner, name, patchName, types, scope: PatchScope.FORERVER);
            return owner;
        }

        public static Type PatchInvitation(this Type owner, string name, params Type[] types)
        {
            ServiceLocator.Instance.CreateInstance<IPatcher>(GetCallerAssembly()).Patch(owner, name, null, types, scope: PatchScope.INVITATION);
            return owner;
        }

        private static Assembly GetCallerAssembly()
        {
            StackTrace stacktrace = new StackTrace();
            var caller = stacktrace.GetFrame(2);
            return caller.GetMethod().DeclaringType.Assembly;
        }

    }


}
