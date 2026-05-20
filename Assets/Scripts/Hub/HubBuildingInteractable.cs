using UnityEngine;

public enum HubBuildingType
{
    Hospital,
    Association,
    BlackMarket
}

[RequireComponent(typeof(BoxCollider2D))]
public class HubBuildingInteractable : MonoBehaviour
{
    [SerializeField] private HubBuildingType buildingType;
    [SerializeField] private CityHubBootstrapper owner;

    public HubBuildingType BuildingType => buildingType;

    public void Configure(CityHubBootstrapper bootstrapper, HubBuildingType type)
    {
        owner = bootstrapper;
        buildingType = type;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null || other.GetComponentInParent<KaisenController>() == null)
        {
            return;
        }

        owner.SetNearbyBuilding(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (owner == null || other.GetComponentInParent<KaisenController>() == null)
        {
            return;
        }

        owner.ClearNearbyBuilding(this);
    }
}
