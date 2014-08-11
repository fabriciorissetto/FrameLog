using FrameLog.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrameLog.Logging
{
    public class DeferredValueMap<TContainer>
    {
        private Dictionary<TContainer, Dictionary<string, Tuple<Func<object>, Func<object>>>> map;

        public DeferredValueMap()
        {
            map = new Dictionary<TContainer, Dictionary<string, Tuple<Func<object>, Func<object>>>>();
        }

        public void Store(TContainer container, string key, Func<object> future, Func<object> past)
        {
            var subMap = getSubmap(container);
            subMap[key] = new Tuple<Func<object>, Func<object>>(future, past);
        }
        public bool HasContainer(TContainer container)
        {
            return map.ContainsKey(container);
        }
        public IDictionary<string, Tuple<object,object>> CalculateAndRetrieve(TContainer container)
        {
            var subMap = map[container];
            var result = new Dictionary<string, Tuple<object, object>>();
            foreach (var kv in subMap)
            {
                try
                {
                    result[kv.Key] = new Tuple<object,object>(kv.Value.Item1(), kv.Value.Item2 == null ? null : kv.Value.Item2());
                }
                catch (Exception e)
                {
                    throw new ErrorInDeferredCalculation(container, kv.Key, e);
                }
            }
            return result;
        }

        private Dictionary<string, Tuple<Func<object>, Func<object>>> getSubmap(TContainer container)
        {
            Dictionary<string, Tuple<Func<object>, Func<object>>> subMap;

            if (!map.TryGetValue(container, out subMap))
            {
                map[container] = subMap = new Dictionary<string, Tuple<Func<object>, Func<object>>>();
            }
            return subMap;
        }
    }
}
