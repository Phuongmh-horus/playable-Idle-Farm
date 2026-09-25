using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FruitConfig", menuName = "Farm/FruitConfig")]
public class FruitConfig : ScriptableObject
{
    public Fruit fruitPrefab;
    public AnimatedFruit animatedFruitPrefab;
    public AnimatedFruitConfig animatedFruitConfig;
}