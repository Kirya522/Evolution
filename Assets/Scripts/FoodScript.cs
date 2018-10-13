﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine;
using Slider = UnityEngine.UI.Slider;


namespace Assets.Scripts
{
    public class FoodScript : MonoBehaviour
    {
        private const int BERRY_INT = (int) CellEnum.TypeOfCell.Berry;

        public void ChangeBerryCount()
        {
            var berryPercent = (int)gameObject.GetComponent<Slider>().value;
            Map.PercentObjects[BERRY_INT] = (float)(berryPercent / 100.0);
            Map.RecalculateSingleTypeObject(BERRY_INT);
        }
    }
}