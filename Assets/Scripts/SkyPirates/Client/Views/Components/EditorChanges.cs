using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DVG.SkyPirates.Client.Views.Components
{
    internal static class EditorChanges
    {
        private const string Editor = "UNITY_EDITOR";
        private static readonly Dictionary<int, Action> _changeSubscriptions;
        private static readonly Dictionary<int, Action> _destroySubscriptions;

        static EditorChanges()
        {
#if UNITY_EDITOR
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            _changeSubscriptions = new();
            _destroySubscriptions = new();
#endif
        }

        [Conditional(Editor)]
        public static void SubscribeOnChanged(Component instance, Action onChanged)
        {
            var instanceId = instance.GetInstanceID();
            if (!_changeSubscriptions.ContainsKey(instanceId))
                _changeSubscriptions[instanceId] = null;
            _changeSubscriptions[instanceId] += onChanged;
        }

        [Conditional(Editor)]
        public static void UnsubscribeOnChanged(Component instance, Action onChanged)
        {
            var instanceId = instance.GetInstanceID();
            if (_changeSubscriptions.ContainsKey(instanceId))
                _changeSubscriptions[instanceId] -= onChanged;
        }

        [Conditional(Editor)]
        public static void SubscribeOnDestroy(GameObject gameObject, Action onChanged)
        {
            var instanceId = gameObject.GetInstanceID();
            if (!_destroySubscriptions.ContainsKey(instanceId))
                _destroySubscriptions[instanceId] = null;
            _destroySubscriptions[instanceId] += onChanged;
        }

        [Conditional(Editor)]
        public static void UnsubscribeOnDestroy(GameObject gameObject, Action onChanged)
        {
            var instanceId = gameObject.GetInstanceID();
            if (_destroySubscriptions.ContainsKey(instanceId))
                _destroySubscriptions[instanceId] -= onChanged;
        }

#if UNITY_EDITOR
        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var eventType = stream.GetEventType(i);
                if (eventType == ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                {
                    stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var data);
                    if (_changeSubscriptions.TryGetValue(data.instanceId, out var subscriptions))
                        subscriptions?.Invoke();
                }
                if (eventType == ObjectChangeKind.DestroyGameObjectHierarchy)
                {
                    stream.GetDestroyGameObjectHierarchyEvent(i, out var data);
                    if (_destroySubscriptions.Remove(data.instanceId, out var subscriptions))
                        subscriptions?.Invoke();
                }
            }
        }
#endif
    }
}
