﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FCG
{
    [CustomEditor(typeof(DayNight))]
    public class DayNightEditor : Editor
    {

        DayNight dayNight;

        bool _moonLight;
        /*
        bool _spotLights;
        bool _streetLights;
        */



        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            dayNight = (DayNight)target;

            if (dayNight.gameObject.activeInHierarchy)
            {

                GUILayout.Space(10);

                if (!dayNight.directionalLight)
                {
                    GUILayout.Label("Warning: You need to set Directional Light");
                    GUILayout.Space(10);
                }



                if (GUILayout.Button("Day"))
                {
                    dayNight.isNight = false;
                }

                if (!dayNight.isNight)
                {

                    if (dayNight.directionalLight)
                    {
                        if (dayNight.intenseSunLight < 50)
                            dayNight.intenseSunLight = 100;

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();



                        GUILayout.Label("Sun Intensity: ");

                        dayNight.intenseSunLight = GUILayout.HorizontalSlider(dayNight.intenseSunLight, 50, 150, GUILayout.Width(200));
                        if (dayNight._intenseSunLight != dayNight.intenseSunLight)
                        {
                            dayNight._intenseSunLight = dayNight.intenseSunLight;
                            dayNight.SetDirectionalLight();
                        }
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(20);

                        EditorGUILayout.BeginHorizontal();

                        dayNight.sunLightColor = EditorGUILayout.ColorField("Sun Light Color", dayNight.sunLightColor);

                        EditorGUILayout.EndHorizontal();

                    }

                    GUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();

                    dayNight.skyColorDay = EditorGUILayout.ColorField("SkyColor", dayNight.skyColorDay);

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();

                    dayNight.equatorColorDay = EditorGUILayout.ColorField("EquatorColor", dayNight.equatorColorDay);

                    EditorGUILayout.EndHorizontal();

                    if (dayNight._skyColorDay != dayNight.skyColorDay || dayNight._equatorColorDay != dayNight.equatorColorDay || dayNight._sunLightColor != dayNight.sunLightColor)
                    {
                        dayNight._skyColorDay = dayNight.skyColorDay;
                        dayNight._equatorColorDay = dayNight.equatorColorDay;
                        dayNight.UpdateColor();
                    }


                }

                GUILayout.Space(5);

                if (GUILayout.Button("Night"))
                {
                    dayNight.isNight = true;
                }

                if (dayNight.night != dayNight.isNight)
                {
                    //Undo.RecordObject(target, "Changed Area Of dayNight");
                    dayNight.night = dayNight.isNight;
                    dayNight.ChangeMaterial();
                }

                if (dayNight.isNight)
                {
                    GUILayout.Space(20);

                    if (dayNight.directionalLight)
                    {


                        EditorGUILayout.BeginHorizontal();

                        dayNight.isMoonLight = GUILayout.Toggle(dayNight.isMoonLight, " MoonLight", GUILayout.Width(240));



                        if (_moonLight != dayNight.isMoonLight)
                        {
                            dayNight.SetDirectionalLight();
                            _moonLight = dayNight.isMoonLight;
                        }

                        GUILayout.Space(10);

                        if (dayNight.isMoonLight)
                        {
                            dayNight.intenseMoonLight = GUILayout.HorizontalSlider(dayNight.intenseMoonLight, 0, 100, GUILayout.Width(120));
                            if (dayNight._intenseMoonLight != dayNight.intenseMoonLight)
                            {
                                dayNight._intenseMoonLight = dayNight.intenseMoonLight;
                                dayNight.SetDirectionalLight();
                            }
                        }

                        GUILayout.Space(20);

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();

                        dayNight.moonLightColor = EditorGUILayout.ColorField("MoonLight Color", dayNight.moonLightColor);

                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(5);

                    }


                    EditorGUILayout.BeginHorizontal();

                    dayNight.skyColorNight = EditorGUILayout.ColorField("SkyColor", dayNight.skyColorNight);

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();

                    dayNight.equatorColorNight = EditorGUILayout.ColorField("EquatorColor", dayNight.equatorColorNight);

                    EditorGUILayout.EndHorizontal();


                    if (dayNight._skyColorNight != dayNight.skyColorNight || dayNight._equatorColorNight != dayNight.equatorColorNight || dayNight._moonLightColor != dayNight.moonLightColor)
                    {
                        dayNight._skyColorNight = dayNight.skyColorNight;
                        dayNight._equatorColorNight = dayNight.equatorColorNight;
                        dayNight.UpdateColor();
                    }


                    GUILayout.Space(10);
                    dayNight.isSpotLights = GUILayout.Toggle(dayNight.isSpotLights, " Street Lights", GUILayout.Width(240));

                    if (dayNight.spotLights != dayNight.isSpotLights)
                    {
                        dayNight.spotLights = dayNight.isSpotLights;
                        dayNight.SetStreetLights();
                    }


                }



                GUILayout.Space(10);

            }

        }
        private void OnEnable()
        {
            dayNight = (DayNight)target;

            if (PrefabUtility.GetPrefabAssetType(dayNight.gameObject) != PrefabAssetType.NotAPrefab)
                PrefabUtility.UnpackPrefabInstance((GameObject)dayNight.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

            if (!dayNight.directionalLight)
            {
                Light directionalLight = FindOrCreateDirectional();

                if (directionalLight)
                    dayNight.directionalLight = directionalLight;
                else
                    Debug.LogWarning("DayNight -> directionalLight is not assigned. It should reference the Directional Light in the scene.");
            }

            dayNight.ChangeMaterial();

        }

        Light FindOrCreateDirectional()
        {
            Light[] allLights = FindObjectsOfType<Light>();
            Light directionalLight = null;

            foreach (Light light in allLights)
            {
                if (light.type == LightType.Directional)
                {
                    if (directionalLight != null)
                    {
                        //Debug.LogWarning("Multiple Directional Lights found");
                        return null; // directionalLight;
                    }

                    directionalLight = light;
                }
            }

            // If no directional light was found, create one
            if (directionalLight == null)
            {
                GameObject newLightObj = new GameObject("Directional Light");
                Light newLight = newLightObj.AddComponent<Light>();
                newLight.type = LightType.Directional;
                newLight.intensity = 1f;
                newLight.shadows = LightShadows.Soft;
                newLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f); // Default rotation

                Debug.Log("No Directional Light found. Created a new one.");
                return newLight;
            }

            return directionalLight;

        }


    }
}