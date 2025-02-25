using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficSimulationTool.Runtime.Util
{
    public static class QueueExtensions
    {
        public static TValue Pop<TValue>([DisallowNull] this IList<TValue> list)
        {
            var item = list.FirstOrDefault();
            list.Remove(item);
            if (item != null)
                return item;
            return default(TValue);
        }

        //Listサイズを増加
        public static void Resize<TValue>([DisallowNull] this List<TValue> list, int size)
        {
            list.Capacity = size;
            list.AddRange(Enumerable.Repeat(default(TValue), size - list.Count));
        }
    }
}
