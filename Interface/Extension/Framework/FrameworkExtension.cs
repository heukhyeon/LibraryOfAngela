using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Extension.Framework
{
    public class FrameworkExtension
    {

        /// <summary>
        /// 인터페이스를 구현하지않았을때의 동작 무시를 위해 <see cref="NotImplementedException"/> 만을 catch 합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="function">수행할 인터페이스의 동작입니다.</param>
        /// <returns>값이 제대로 정의된경우 not null, 정의되지않았거나 정의됬어도 null 인경우 null</returns>
        public static T GetSafeAction<T>(Func<T> function) where T : class
        {
            try
            {
                return function();
            }
            catch (NotImplementedException)
            {
                return null;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
            catch (MissingFieldException)
            {
                return null;
            }
        }

    }
}
