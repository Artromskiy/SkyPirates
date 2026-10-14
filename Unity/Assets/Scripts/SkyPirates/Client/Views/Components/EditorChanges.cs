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
        private static readonly Dictionary<ulong, Action> _changeSubscriptions;

        static EditorChanges()
        {
#if UNITY_EDITOR
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            _changeSubscriptions = new();
#endif
        }

        [Conditional(Editor)]
        public static void SubscribeOnChanged(Component instance, Action onChanged)
        {
            ulong instanceId = EntityId.ToULong(instance.GetEntityId());
            if (!_changeSubscriptions.ContainsKey(instanceId))
                _changeSubscriptions[instanceId] = null;
            _changeSubscriptions[instanceId] += onChanged;
        }

        [Conditional(Editor)]
        public static void UnsubscribeOnChanged(Component instance, Action onChanged)
        {
            ulong instanceId = EntityId.ToULong(instance.GetEntityId());
            if (_changeSubscriptions.ContainsKey(instanceId))
                _changeSubscriptions[instanceId] -= onChanged;
        }

#if UNITY_EDITOR
        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                    continue;

                stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var data);
                if (_changeSubscriptions.TryGetValue(EntityId.ToULong(data.entityId), out var subscriptions))
                    subscriptions?.Invoke();
            }
        }
#endif
    }
}
