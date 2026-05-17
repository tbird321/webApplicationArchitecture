using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace WebApplicationArch.Tests.TestModels
{
    public static class ModelComparer
    {
        public static bool AreEqual<T>(T obj1, T obj2)
        {
            if (obj1 == null && obj2 == null)
                return true;
            if (obj1 == null || obj2 == null)
                return false;

            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                // Skip indexed properties
                if (property.GetIndexParameters().Length > 0)
                    continue;

                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) &&
                    property.PropertyType != typeof(string))
                {
                    var obj1Value = property.GetValue(obj1) as IEnumerable;
                    var obj2Value = property.GetValue(obj2) as IEnumerable;

                    if (!CompareEnumerables(obj1Value, obj2Value))
                        return false;
                }
                else
                {
                    var obj1Value = property.GetValue(obj1);
                    var obj2Value = property.GetValue(obj2);

                    if (!CompareValues(obj1Value, obj2Value))
                        return false;
                }
            }
            return true;
        }

        private static bool CompareValues(object obj1, object obj2)
        {
            // Handle null values
            if (obj1 == null && obj2 == null)
                return true;
            if (obj1 == null || obj2 == null)
                return false;

            // Handle primitive types and strings
            if (obj1.GetType().IsPrimitive || obj1 is string)
            {
                return obj1.Equals(obj2);
            }

            // Handle DateTime
            if (obj1 is DateTime dt1 && obj2 is DateTime dt2)
            {
                return dt1.Equals(dt2);
            }

            // Handle complex objects recursively
            if (obj1.GetType() == obj2.GetType())
            {
                // Use reflection to compare complex objects
                var method = typeof(ModelComparer).GetMethod("AreEqual", BindingFlags.Public | BindingFlags.Static);
                var genericMethod = method.MakeGenericMethod(obj1.GetType());
                return (bool)genericMethod.Invoke(null, new object[] { obj1, obj2 });
            }

            // Fallback to Equals for other types
            return obj1.Equals(obj2);
        }

        private static bool CompareEnumerables(IEnumerable obj1, IEnumerable obj2)
        {
            if (obj1 == null && obj2 == null)
                return true;
            if (obj1 == null || obj2 == null)
                return false;

            var enumerator1 = obj1.GetEnumerator();
            var enumerator2 = obj2.GetEnumerator();

            while (enumerator1.MoveNext())
            {
                if (!enumerator2.MoveNext() || !CompareValues(enumerator1.Current, enumerator2.Current))
                    return false;
            }

            if (enumerator2.MoveNext()) // obj2 has more items
                return false;

            return true;
        }
    }
}