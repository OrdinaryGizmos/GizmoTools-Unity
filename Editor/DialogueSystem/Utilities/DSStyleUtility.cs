using UnityEditor;
using UnityEngine.UIElements;

namespace OrdinaryGizmos.GizmoTools.DS.Utilities
{
    public static class DSStyleUtility
    {
        public static VisualElement AddClasses(this VisualElement element, params string[] classNames)
        {
            foreach (string className in classNames)
            {
                element.AddToClassList(className);
            }

            return element;
        }

        internal const string AssetPath = "Packages/com.ordinarygizmos.gizmotools/Editor/DialogueSystem/Stylesheets/";
        public static VisualElement AddStyleSheets(this VisualElement element, params string[] styleSheetNames)
        {
            foreach (string styleSheetName in styleSheetNames)
            {
                StyleSheet stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetPath + styleSheetName);


                element.styleSheets.Add(stylesheet);
            }

            return element;
        }
    }
}