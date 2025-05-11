#nullable enable
using DVG.SkyPirates.Client.IViews;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class CardsView : MonoBehaviour, ICardsView
    {
        public event Action<int>? OnCardClicked;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                OnCardClicked?.Invoke(0);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                OnCardClicked?.Invoke(1);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                OnCardClicked?.Invoke(2);

            if (Input.GetKeyDown(KeyCode.Alpha4))
                OnCardClicked?.Invoke(3);
        }
    }
}
