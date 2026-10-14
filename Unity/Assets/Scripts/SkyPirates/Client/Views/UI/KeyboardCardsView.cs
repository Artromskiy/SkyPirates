#nullable enable
using SkyPirates.Client.IViewModels;
using SkyPirates.Client.IViews;
using UnityEngine;

namespace SkyPirates.Client.Views
{
    public class KeyboardCardsView : View<ICardsVM>
    {
        public override void OnInject() { }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                ViewModel.UseCard(0);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                ViewModel.UseCard(1);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                ViewModel.UseCard(2);
        }
    }
}
