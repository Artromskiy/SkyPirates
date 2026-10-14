using Delta.ECS.Unity;
using SkyPirates.Shared.Components.Runtime;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    internal static class EntityViewLifetime
    {
        private static readonly ConditionalWeakTable<GameObject, ViewState> States = new();

        public static bool TryEnterUpdate(MonoBehaviour view, EntityContext context)
        {
            if (!context.IsAlive)
            {
                Object.Destroy(view.gameObject);
                return false;
            }

            ViewState state = States.GetValue(view.gameObject, CreateState);
            if (!context.Has<SyncId>())
            {
                if (!state.Hidden)
                {
                    state.Scale = view.transform.localScale;
                    view.transform.localScale = Vector3.zero;
                    state.Hidden = true;
                }

                return false;
            }

            if (state.Hidden)
            {
                view.transform.localScale = state.Scale;
                state.Hidden = false;
            }
            else
            {
                state.Scale = view.transform.localScale;
            }

            return true;
        }

        private static ViewState CreateState(GameObject view) => new(view.transform.localScale);

        private sealed class ViewState
        {
            public Vector3 Scale;
            public bool Hidden;

            public ViewState(Vector3 scale) => Scale = scale;
        }
    }
}
