using DVG.Core;
using DVG.SkyPirates.Client.IFactories;
using UnityEngine;

namespace Assets.Scripts.SkyPirates.Client.Factories
{
    public class PathViewFactory<T> : IPathViewFactory<T>
        where T : IView
    {
        public T Create(string parameters)
        {
            var prefab = Resources.Load<GameObject>(parameters);
            var go = Object.Instantiate(prefab);
            var view = go.GetComponent<T>();
            return view;
        }
    }
}
