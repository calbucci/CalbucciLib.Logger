using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CalbucciLib.Tests
{
    static public class CompareUtils
    {
        static public bool AreEqual(IDictionary dict1, IDictionary dict2, bool listOrderMatters = true)
        {
            if (dict1 == null && dict2 == null)
                return true;
            if (dict1 == null || dict2 == null)
                return false;
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var k1 in dict1.Keys)
            {
                var v1 = dict1[k1];
                if (!dict2.Contains(k1))
                    return false;
                object v2 = dict2[k1];
                if (!AreEqual(v1, v2, listOrderMatters))
                    return false;
            }
            return true;
        }

        static public bool AreEqual(IList list1, IList list2, bool listOrderMatters = true)
        {
            if (list1 == null && list2 == null)
                return true;
            if (list1 == null || list2 == null)
                return false;
            if (list1.Count != list2.Count)
                return false;

            if (listOrderMatters)
            {
                for (int i = 0; i < list1.Count; i++)
                {
                    var v1 = list1[i];
                    var v2 = list2[i];
                    if (!AreEqual(v1, v2, listOrderMatters))
                        return false;
                }
            }
            else
            {
                foreach (var v1 in list1)
                {
                    bool found = false;
                    for (int i = 0; i < list2.Count; i++)
                    {
                        if (AreEqual(v1, list2[i], listOrderMatters))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        return false;
                }
            }
            return true;
        }


        static public bool AreEqual(object o1, object o2, bool listOrderMatters = true)
        {
            if (o1 == null && o2 == null)
                return true;
            if (o1 == null || o2 == null)
                return false;

            // De-Jsonify it
            if (o1 is JValue)
                o1 = ((JValue) o1).Value;
            if (o2 is JValue)
                o2 = ((JValue) o2).Value;

            var o1type = o1.GetType();
            var o2type = o2.GetType();
            if (o1type != o2type)
            {
                // for numbers, we try to compare the numeric value
                if (IsNumericType(o1type) && IsNumericType(o2type))
                {
                    return AreNumericEqual(o1, o2);
                }
            }

            if (o1 is IDictionary)
            {
                return AreEqual(o1 as IDictionary, o2 as IDictionary, listOrderMatters);
            }
            else if (o1 is IList)
            {
                return AreEqual(o1 as IList, o2 as IList, listOrderMatters);
            }
            

            

            return o1.Equals(o2);
        }

        static public bool AreNumericEqual(object o1, object o2)
        {
            var o1type = o1.GetType();
            var o2type = o2.GetType();

            // If at least one number is a floating point, convert to decimal and compare
            if ((o1type == typeof(float) || o1type == typeof(double) || o1type == typeof(decimal))
                || (o2type == typeof(float) || o2type == typeof(double) || o2type == typeof(decimal)))
            {
                return ToDecimal(o1) == ToDecimal(o2);
            }

            // If both numbers are unsigned integers, convert and compare
            if ((o1type == typeof(byte) || o1type == typeof(UInt16) || o1type == typeof(UInt32) ||
                 o1type == typeof(UInt64))
                &&
                (o2type == typeof(byte) || o2type == typeof(UInt16) || o2type == typeof(UInt32) ||
                 o2type == typeof(UInt64)))
            {
                return ToULong(o1) == ToULong(o2);
            }

            // If both numbers are signed integers, convert and compare
            if ((o1type == typeof(byte) || o1type == typeof(UInt16) || o1type == typeof(UInt32) ||
                 o1type == typeof(UInt64))
                &&
                (o2type == typeof(byte) || o2type == typeof(UInt16) || o2type == typeof(UInt32) ||
                 o2type == typeof(UInt64)))
            {
                return ToLong(o1) == ToLong(o2);
            }

            // If we have a mix of signed & unsigned, let's be conservative and compare as signed integer by fail for negative numbers
            var l1 = ToLong(o1);
            var l2 = ToLong(o2);
            if (l1 < 0 || l2 < 0)
                return false; //  one of them isn't right

            return l1 == l2;
        }

        static public decimal ToDecimal(object o)
        {
            if (o == null)
                return 0;

            var oc = o as IConvertible;
            if (oc == null)
                return 0;

            return oc.ToDecimal(null);
        }

        static public long ToLong(object o)
        {
            if (o == null)
                return 0;

            var oc = o as IConvertible;
            if (oc == null)
                return 0;

            return oc.ToInt64(null);
        }

        static public ulong ToULong(object o)
        {
            if (o == null)
                return 0;

            var oc = o as IConvertible;
            if (oc == null)
                return 0;

            return oc.ToUInt64(null);
        }

        public static bool IsNumericType(Type type)
        {
            // From: http://stackoverflow.com/questions/124411/using-net-how-can-i-determine-if-a-type-is-a-numeric-valuetype
            if (type == null)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsNumericType(Nullable.GetUnderlyingType(type));
                    }
                    return false;
            }
            return false;
        }
    }
}
