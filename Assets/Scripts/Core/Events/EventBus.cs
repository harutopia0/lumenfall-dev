using System;
using System.Collections.Generic;

namespace Lumenfall.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> subscribers = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            Type eventType = typeof(T);

            if (!subscribers.TryGetValue(eventType, out var list))
            {
                list = new List<Delegate>();
                subscribers[eventType] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            Type eventType = typeof(T);

            if (subscribers.TryGetValue(eventType, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    subscribers.Remove(eventType);
                }
            }
        }

        public static void Publish<T>(T eventData)
        {
            Type eventType = typeof(T);

            if (subscribers.TryGetValue(eventType, out var list))
            {
                Delegate[] invocationList = list.ToArray();
                for (int i = 0; i < invocationList.Length; i++)
                {
                    if (invocationList[i] is Action<T> action)
                    {
                        try
                        {
                            action.Invoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"[EventBus] Error dispatching event {eventType.Name}: {ex}");
                        }
                    }
                }
            }
        }

        public static void Clear()
        {
            subscribers.Clear();
        }
    }
}
