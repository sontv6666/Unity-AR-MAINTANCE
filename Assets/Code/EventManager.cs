using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Code
{
    public class EventManager : MonoBehaviour
    {
        private static Dictionary<string, UnityEvent> eventDictionary = new Dictionary<string, UnityEvent>();

        public static void StartListening(string eventName, UnityAction listener)
        {
            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                eventDictionary.Add(eventName, thisEvent);
            }
        }

        public static void StopListening(string eventName, UnityAction listener)
        {
            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }

        public static void TriggerEvent(string eventName)
        {
            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                Debug.Log($"📣 Triggering event: {eventName}");
                thisEvent.Invoke();
            }
            else
            {
                Debug.LogWarning($"⚠️ Attempted to trigger event {eventName} but no listeners registered");
            }
        }
    }
}