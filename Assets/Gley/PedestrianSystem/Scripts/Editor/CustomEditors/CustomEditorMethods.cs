using System.Reflection;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    /// <summary>
    /// Common methods for editor scripts.
    /// </summary>
    internal static class CustomEditorMethods
    {
        internal static FieldInfo GetPrivateField<T>(string fieldName)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError($"Variable {fieldName} does not exists.");
                return null;
            }
            return field;
        }
    }
}