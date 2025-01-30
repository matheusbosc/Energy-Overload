using TMPro;
using Unity.Collections;
using UnityEngine;

namespace com.matheusbosc.energyoverload
{
    [CreateAssetMenu(fileName = "New Building", menuName = "New Building", order = 0)]
    public class BuildingInfo : ScriptableObject
    {
        public BuildingType buildingType;
        public string buildingName;
        public float maxPowerUsage;
        public Sprite icon;
        
    }

    public enum BuildingType
    {
        SingleStoryHome,
        DoubleStoryHome,
        Townhouse,
        Apartment,
        SmallShop,
        Supermarket,
        Utilities
    }
}