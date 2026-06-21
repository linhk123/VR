using UnityEngine;

// ĐỊNH NGHĨA SHAPETYPE Ở ĐÂY ĐỂ TRÁNH LỖI CS0246
public enum ShapeType 
{
    Cube,
    Cylinder,
    Sphere,
    Pyramid
}

[CreateAssetMenu(fileName = "NewOrder", menuName = "GeoForge/Order")]
public class Order : ScriptableObject 
{
    public string orderID;
    
    [TextArea(3, 5)]
    public string dialog; 
    
    public ShapeType targetShape; 
    public float targetVolume; 
    public float tolerancePercent = 5f; 
    public int rewardGold; 
}