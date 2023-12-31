﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InfinityCode.UltimateEditorEnhancer.HierarchyTools
{
    [InitializeOnLoad]
    public static class BestIconDrawer
    {
        private const double CacheLifeTimeSec = 5;
        
        private static Texture _prefabIcon;
        private static Texture _unityLogoTexture;
        private static HashSet<int> hierarchyWindows;
        private static bool inited = false;
        private static Dictionary<int, CachedTexture> cachedTextures = new Dictionary<int, CachedTexture>();

        private static Texture prefabIcon
        {
            get
            {
                if (_prefabIcon == null) _prefabIcon = EditorIconContents.prefab.image;
                return _prefabIcon;
            }
        }

        private static Texture unityLogoTexture
        {
            get
            {
                if (_unityLogoTexture == null) _unityLogoTexture = EditorIconContents.unityLogo.image;
                return _unityLogoTexture;
            }
        }

        static BestIconDrawer()
        {
            hierarchyWindows = new HashSet<int>();
            HierarchyItemDrawer.Register("BestIconDrawer", DrawItem, HierarchyToolOrder.BestIcon);
        }

        private static void DrawItem(HierarchyItem item)
        {
            if (!Prefs.hierarchyOverrideMainIcon) return;
            if (!inited) Init();

            Event e = Event.current;

            if (e.type == EventType.Layout)
            {
                EditorWindow lastHierarchyWindow = SceneHierarchyWindowRef.GetLastInteractedHierarchy();
                int wid = lastHierarchyWindow.GetInstanceID();
                if (!hierarchyWindows.Contains(wid)) InitWindow(lastHierarchyWindow, wid);
                return;
            }

            if (e.type != EventType.Repaint) return;

            Texture texture;
            if (!GetTexture(item, out texture)) return;
            if (texture == null) return;

            const int iconSize = 16;

            Rect rect = item.rect;
            Rect iconRect = new Rect(rect) {width = iconSize, height = iconSize};
            iconRect.y += (rect.height - iconSize) / 2;
            GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
        }

        public static Texture GetGameObjectIcon(GameObject go)
        {
            if (go.tag == "Collection")
            {
                return Icons.collection;
            }

            Texture texture = AssetPreview.GetMiniThumbnail(go);
            string textureName = texture.name;

            if (textureName == "d_Prefab Icon" || textureName == "Prefab Icon")
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(go)) return prefabIcon;
            }
            else if (textureName != "d_GameObject Icon" && textureName != "GameObject Icon")
            {
                return texture;
            }

            Component[] components = go.GetComponents<Component>();
            Component best;
            if (components.Length > 1)
            {
                best = components[1];
                if (components.Length > 2)
                {
                    if (best is CanvasRenderer)
                    {
                        best = components[2];
                        if (best is UnityEngine.UI.Image && components.Length > 3)
                        {
                            Component c = components[3];
                            texture = AssetPreview.GetMiniThumbnail(c);
                            if (texture != null)
                            {
                                textureName = texture.name;
                                if (textureName != "cs Script Icon" && textureName != "d_cs Script Icon") best = c;
                            }
                        }
                    }
                }
            }
            else best = components[0];

            texture = AssetPreview.GetMiniThumbnail(best);

            if (texture == null) return EditorIconContents.gameObject.image;
            return texture;
        }

        private static bool GetTexture(HierarchyItem item, out Texture texture)
        {
            texture = null;
            CachedTexture cachedTexture;
            if (cachedTextures.TryGetValue(item.id, out cachedTexture))
            {
                if (EditorApplication.timeSinceStartup - cachedTexture.time < CacheLifeTimeSec)
                {
                    texture = cachedTexture.texture;
                    return true; 
                }
                cachedTextures.Remove(item.id);
            }

            if (item.gameObject != null)
            {
                texture = GetGameObjectIcon(item.gameObject);
                cachedTextures.Add(item.id, new CachedTexture
                {
                    texture = texture,
                    time = EditorApplication.timeSinceStartup
                });
            }
            else if (item.target == null) texture = unityLogoTexture;
            else return false;

            return true;
        }

        private static void Init()
        {
            inited = true;
            Object[] windows = UnityEngine.Resources.FindObjectsOfTypeAll(SceneHierarchyWindowRef.type);
            foreach (Object window in windows)
            {
                int wid = window.GetInstanceID();
                if (!hierarchyWindows.Contains(wid)) InitWindow(window as EditorWindow, wid);
            }
        }

        private static void InitWindow(EditorWindow lastHierarchyWindow, int wid)
        {
            if (float.IsNaN(lastHierarchyWindow.rootVisualElement.worldBound.width)) return;

            IMGUIContainer container = lastHierarchyWindow.rootVisualElement.parent.Query<IMGUIContainer>().First();
            container.onGUIHandler = (() => OnGUIBefore(wid)) + container.onGUIHandler;
            HierarchyHelper.SetDefaultIconsSize(lastHierarchyWindow);
            hierarchyWindows.Add(wid);
        }

        private static void OnGUIBefore(int wid)
        {
            if (!Prefs.hierarchyOverrideMainIcon) return;
            if (Event.current.type != EventType.Layout) return;

            EditorWindow w = EditorUtility.InstanceIDToObject(wid) as EditorWindow;
            if (w != null) HierarchyHelper.SetDefaultIconsSize(w);
        }
        
        internal class CachedTexture
        {
            public Texture texture;
            public double time;
        }
    }
}