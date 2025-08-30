using Gley.UrbanSystem.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianTypesWindow : SetupWindowBase
    {
        private readonly float _scrollAdjustment = 205;

        private List<string> _pedestrianCategories = new List<string>();
        private string _errorText;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            _errorText = "";
            LoadPedestrians();
            return base.Initialize(windowProperties, window);
        }


        protected override void TopPart()
        {
            base.TopPart();
            EditorGUILayout.LabelField("Pedestrian types are used to limit movement.\n" +
                "You can use different pedestrian types to restrict the access in some areas.");
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));

            for (int i = 0; i < _pedestrianCategories.Count; i++)
            {
                GUILayout.BeginHorizontal();
                _pedestrianCategories[i] = EditorGUILayout.TextField(_pedestrianCategories[i]);
                _pedestrianCategories[i] = Regex.Replace(_pedestrianCategories[i], @"^[\d-]*\s*", "");
                _pedestrianCategories[i] = _pedestrianCategories[i].Replace(" ", "");
                _pedestrianCategories[i] = _pedestrianCategories[i].Trim();
                if (GUILayout.Button("Remove"))
                {
                    _pedestrianCategories.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Pedestrian Category"))
            {
                _pedestrianCategories.Add("");
            }

            GUILayout.EndScrollView();
        }


        protected override void BottomPart()
        {
            GUILayout.Label(_errorText);
            if (GUILayout.Button("Save"))
            {
                if (CheckForNull() == false)
                {
                    _errorText = "Success";
                    Save();
                }
            }
            EditorGUILayout.Space();
            base.BottomPart();
        }


        private void LoadPedestrians()
        {
            var allpedestrianypes = Enum.GetValues(typeof(PedestrianTypes)).Cast<PedestrianTypes>();
            foreach (PedestrianTypes pedestrian in allpedestrianypes)
            {
                _pedestrianCategories.Add(pedestrian.ToString());
            }
        }


        private void Save()
        {
            FileCreator.CreateAgentTypesFile<PedestrianTypes>(_pedestrianCategories, Internal.PedestrianSystemConstants.GLEY_PEDESTRIAN_SYSTEM, Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespace,Gley.PedestrianSystem.Internal.PedestrianSystemConstants.AgentTypesPath);
        }


        private bool CheckForNull()
        {
            for (int i = 0; i < _pedestrianCategories.Count - 1; i++)
            {
                for (int j = i + 1; j < _pedestrianCategories.Count; j++)
                {
                    if (_pedestrianCategories[i] == _pedestrianCategories[j])
                    {
                        _errorText = _pedestrianCategories[i] + " Already exists. No duplicates allowed";
                        return true;
                    }
                }
            }
            for (int i = 0; i < _pedestrianCategories.Count; i++)
            {
                if (string.IsNullOrEmpty(_pedestrianCategories[i]))
                {
                    _errorText = "Car category cannot be empty! Please fill all of them";
                    return true;
                }
            }
            return false;
        }
    }
}
