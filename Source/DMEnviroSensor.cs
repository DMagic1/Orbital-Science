using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DMagic
{
    class DMEnviroSensor : ModuleEnviroSensor
    {
        private float min = 0f;
        private float max = 1f;
        private Transform indicator = null;
        private Transform rotor1 = null;
        private Transform rotor2 = null;
        private Transform rotor3 = null;
        private Transform tilt1 = null;
        private Vector3 indicatorAxis = Vector3.right;
        private double indicatorPosition = 0;
        private int sensorInt = 0;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Flying)
            {
                sensorInt = sensorTypeInt(sensorType);
                if (sensorInt == 1 || sensorInt == 2 || sensorInt == 3) indicator = part.FindModelTransform(sensorType);
                if (sensorInt == 3) indicatorPosition = indicator.localPosition.y;
                if (sensorInt == 4)
                {
                    rotor1 = part.FindModelTransform(sensorType + ".000");
                    rotor2 = part.FindModelTransform(sensorType + ".001");
                    rotor3 = part.FindModelTransform(sensorType + ".002");
                    tilt1 = part.FindModelTransform(sensorType + ".003");
                }
            }
        }

        private int sensorTypeInt(string type)
        {
            switch (type)
            {
                case "PRES":
                    return 1;
                case "GRAV":
                    return 2;
                case "TEMP":
                    return 3;
                case "ACC":
                    return 4;
                default:
                    return 0;
            }
        }

        public void Update()
        {
            if (sensorActive)
            {
                animateIndicator();
            }
        }

        private void animateIndicator()
        {
            if (indicator != null)
            {
                float maxSensorValue = 0f;
                float currentSensorValue = 0f;
                float normSensorValue = 0f;

                maxSensorValue = sensorValue(sensorInt);
                currentSensorValue = parseSensor();
                normSensorValue = Mathf.Clamp(currentSensorValue / maxSensorValue, 0f, 1f);

                if (sensorInt == 1 || sensorInt == 2) indicator.localRotation = Quaternion.Euler(indicatorAxis * Mathf.Lerp(min, max, normSensorValue));
                if (sensorInt == 3) indicator.Translate(0f, normSensorValue, 0f);
                if (sensorInt == 4)
                {
                    rotor1.Rotate(0f, 100 * TimeWarp.deltaTime, 0f);
                    rotor2.Rotate(0f, 100 * normSensorValue * TimeWarp.deltaTime, 0f);
                    rotor3.Rotate(0f, 100 * normSensorValue * TimeWarp.deltaTime, 0f);
                    tilt1.localRotation = Quaternion.Euler(indicatorAxis * Mathf.Lerp(-15f, 15f, normSensorValue));
                }
            }
        }

        private float parseSensor()
        {
            float parseValue = 0f;
            if (float.TryParse(readoutInfo, out parseValue)) return parseValue;
            else return parseValue;
        }

        private float sensorValue(int type)
        {
            switch (type)
            {
                case 1:
                    return 100;
                case 2:
                    return 100;
                case 3:
                    return 500;
                case 4:
                    return 50;
                default:
                    return 1;
            }
        }

    }
}
