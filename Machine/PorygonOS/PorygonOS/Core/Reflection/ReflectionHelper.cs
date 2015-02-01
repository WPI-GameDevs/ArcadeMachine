using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Reflection
{
    public class ReflectionHelper
    {
        /// <summary>
        /// Gets all types with a certain attribute
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
        /// <param name="inherit">Should this attribute be inherited</param>
        /// <returns>An enumerable</returns>
        public static IEnumerable<Type> GetTypesWithAttribute(Type attributeType, bool inherit = true)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach(Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach(Type t in types)
                {
                    if (t.IsDefined(attributeType, inherit))
                        yield return t;
                }
            }
        }
    }
}
