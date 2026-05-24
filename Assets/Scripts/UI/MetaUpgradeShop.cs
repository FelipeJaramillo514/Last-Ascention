using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MetaUpgradeShop : UIModalPanel
{
    private sealed class UpgradeDefinition
    {
        public string key;
        public string name;
        public string description;
        public int cost;
    }

    [Header("Market")]
    [SerializeField] private Text walletText;
    [SerializeField] private Text hintText;
    [SerializeField] private MetaUpgradeCardView[] upgradeCards = new MetaUpgradeCardView[5];

    private readonly List<UpgradeDefinition> definitions = new List<UpgradeDefinition>
    {
        new UpgradeDefinition { key = "vitalidad_extra", name = "Vitalidad extra", description = "+5 MaxHP base", cost = 500 },
        new UpgradeDefinition { key = "slot_sombra", name = "Slot de sombra", description = "+1 slot permanente", cost = 1000 },
        new UpgradeDefinition { key = "filo_eterno", name = "Filo eterno", description = "+5% dano base de espada", cost = 800 },
        new UpgradeDefinition { key = "reflejos", name = "Reflejos", description = "-0.1s cooldown dodge", cost = 600 },
        new UpgradeDefinition { key = "percepcion_aguda", name = "Percepcion aguda", description = "+3 Perception base", cost = 700 },
    };

    public bool IsVisible => IsOpen;

    public void Show()
    {
        Open();
    }

    public void Hide()
    {
        Close();
    }

    protected override void OnOpened()
    {
        Refresh();
        if (hintText != null)
        {
            hintText.text = "ESC — Cerrar";
        }
    }

    protected override void ValidateEditorReferences()
    {
        base.ValidateEditorReferences();
        if (walletText == null || upgradeCards == null || upgradeCards.Length < definitions.Count)
        {
            Debug.LogError("[MetaUpgradeShop] Asigna wallet y 5 tarjetas en el prefab.", this);
        }
    }

    public void Refresh()
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            if (i >= upgradeCards.Length || upgradeCards[i] == null)
            {
                continue;
            }

            UpgradeDefinition definition = definitions[i];
            MetaUpgradeCardView card = upgradeCards[i];
            if (card.title == null || card.button == null)
            {
                continue;
            }

            int purchaseCount = PersistentData.Instance.GetUpgradePurchaseCount(definition.key);
            card.title.text = definition.name;
            if (card.description != null) card.description.text = definition.description;
            if (card.price != null) card.price.text = definition.cost + " cristales";
            if (card.tier != null) card.tier.text = "Comprado: " + purchaseCount + " / III";
            card.button.interactable = purchaseCount < 3 && PersistentData.Instance.SpendableGold >= definition.cost;
            if (card.buttonLabel != null)
            {
                card.buttonLabel.text = purchaseCount >= 3 ? "MAX" : "Comprar";
            }

            card.button.onClick.RemoveAllListeners();
            card.button.onClick.AddListener(() =>
            {
                if (PersistentData.Instance.GetUpgradePurchaseCount(definition.key) >= 3)
                {
                    return;
                }

                if (PersistentData.Instance.PurchaseUpgrade(definition.key, definition.cost))
                {
                    if (GameStateManager.Instance != null)
                    {
                        GameStateManager.Instance.SaveCurrentState();
                    }

                    Refresh();
                }
            });
        }

        if (walletText != null)
        {
            walletText.text = "Fondos: " + Mathf.RoundToInt(PersistentData.Instance.SpendableGold) + " cristales";
        }
    }
}
