using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    public struct ServiceKey
    {
        public Type type;
        public object additionalKey;
    }

    public class ServiceLocator : Singleton<ServiceLocator>
    {
        private Dictionary<Type, Func<ServiceKey, object>> creatorDics = new Dictionary<Type, Func<ServiceKey, object>>();
        
        private Dictionary<ServiceKey, object> savedInstances = new Dictionary<ServiceKey, object>();

        public void inject<T>(Func<ServiceKey, T> creator) where T : class
        {
            creatorDics[typeof(T)] = creator;
        }

        public T GetInstance<T>() where T : class
        {
            var key = new ServiceKey { type = typeof(T) };
            if (!creatorDics.ContainsKey(typeof(T)))
            {
                throw new Exception($"From ServiceLocator :: Type is Not Defined, Please Check ==> ${typeof(T).FullName}");
            }
            if (savedInstances.ContainsKey(key))
            {
                return (T) savedInstances[key];
            }
            var instance = creatorDics[typeof(T)](key);
            savedInstances[key] = instance;
            return (T) instance;
        }

        public T CreateInstance<T>(object key) where T : class
        {
            var k = new ServiceKey { type = typeof(T), additionalKey = key };
            if (!creatorDics.ContainsKey(typeof(T)))
            {
                throw new Exception($"From ServiceLocator :: Type is Not Defined, Please Check ==> ${typeof(T).FullName}");
            }
            if (savedInstances.ContainsKey(k))
            {
                return (T)savedInstances[k];
            }
            var instance = creatorDics[typeof(T)](k);
            savedInstances[k] = instance;
            return (T)instance;
        }

    }
}
