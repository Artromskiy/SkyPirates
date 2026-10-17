using Common.UI;
using SlyPirates.Client.Views;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;
using System;
using TMPro;
using UnityEngine;

namespace SlyPirates.Meta
{
    public sealed class TeamSelectionScreen : MonoBehaviour
    {
        [SerializeField]
        private Canvas _screenCanvas;
        [SerializeField]
        private Transform _teamCardsRoot;
        [SerializeField]
        private Transform _collectionCardsRoot;
        [SerializeField]
        private TMP_Text _teamCount;
        [SerializeField]
        private TMP_Text _hint;
        [SerializeField]
        private EventButton _doneButton;
        [SerializeField]
        private UnitCardView _cardPrefab;

        private static TeamSelectionScreen _instance;

        private void Awake()
        {
            _instance = this;
            TouchCardsView[] embeddedGameplayCards = GetComponentsInChildren<TouchCardsView>(true);
            for (int index = 0; index < embeddedGameplayCards.Length; index++)
            {
                embeddedGameplayCards[index].gameObject.SetActive(false);
            }

            _screenCanvas.enabled = false;
            if (_doneButton != null)
            {
                _doneButton.OnClick += OnDoneClicked;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void Open()
        {
            _instance._screenCanvas.enabled = true;
            _instance.Refresh();
        }

        private void Refresh()
        {
            if (_teamCardsRoot == null || _collectionCardsRoot == null
                || _teamCount == null || _hint == null || _doneButton == null || _cardPrefab == null)
            {
                return;
            }

            Clear(_teamCardsRoot);
            Clear(_collectionCardsRoot);

            UnitId[] team = ClientSetupData.TeamCards;
            UnitId[] collection = ClientSetupData.CollectionCards;
            _teamCount.text = $"{team.Length}/{UnitDeck.Capacity}";
            _hint.text = team.Length == UnitDeck.Capacity ? string.Empty : "Добавьте карты из коллекции, чтобы продолжить.";
            _doneButton.IsInteractable = ClientSetupData.HasFullTeam;

            for (int index = 0; index < team.Length; index++)
            {
                UnitId unitId = team[index];
                AddCard(_teamCardsRoot, unitId, () =>
                {
                    ClientSetupData.Pack.MoveToCollection(unitId);
                    Refresh();
                });
            }

            for (int index = 0; index < collection.Length; index++)
            {
                UnitId unitId = collection[index];
                AddCard(_collectionCardsRoot, unitId, () =>
                {
                    ClientSetupData.Pack.MoveToTeam(unitId);
                    Refresh();
                });
            }
        }

        private void AddCard(Transform parent, UnitId unitId, Action onClick)
        {
            UnitCardView card = Instantiate(_cardPrefab, parent);
            card.Init(unitId);
            card.OnClick += onClick;
        }

        private void OnDoneClicked()
        {
            if (ClientSetupData.HasFullTeam)
            {
                _screenCanvas.enabled = false;
            }
        }

        private static void Clear(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Destroy(parent.GetChild(index).gameObject);
            }
        }
    }
}
