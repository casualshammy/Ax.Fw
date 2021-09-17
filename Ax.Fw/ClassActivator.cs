using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ax.Fw
{
    public class ClassActivator
    {
        //public Dictionary<Type, object> GetClasses<TAttribute>() where TAttribute : Attribute
        //{
        //    var dictionary = new Dictionary<Type, object>();
        //    foreach (var type in GetTypesWith<TAttribute>(true))
        //    {
        //    }
        //}

        IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit) where TAttribute : Attribute
        {
            return from a in AppDomain.CurrentDomain.GetAssemblies()
                   from t in a.GetTypes()
                   where t.IsDefined(typeof(TAttribute), inherit)
                   select t;
        }
    }
}
