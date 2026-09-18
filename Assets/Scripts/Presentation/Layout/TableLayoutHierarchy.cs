using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public static class TableLayoutHierarchy
    {
        public static string GetPath(Transform root, Transform target)
        {
            if (target == null || root == null)
                return string.Empty;

            if (target == root)
                return ".";

            var segments = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                segments.Push($"{current.name}#{current.GetSiblingIndex()}");
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        public static Transform Find(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            if (path == ".")
                return root;

            var segments = path.Split('/');
            var current = root;
            for (var i = 0; i < segments.Length; i++)
            {
                if (current == null)
                    return null;

                var segment = segments[i];
                var hash = segment.LastIndexOf('#');
                if (hash <= 0)
                    return null;

                var name = segment.Substring(0, hash);
                if (!int.TryParse(segment.Substring(hash + 1), out var siblingIndex))
                    return null;

                Transform match = null;
                var sibling = 0;
                for (var c = 0; c < current.childCount; c++)
                {
                    var child = current.GetChild(c);
                    if (child.name != name)
                        continue;

                    if (sibling == siblingIndex)
                    {
                        match = child;
                        break;
                    }

                    sibling++;
                }

                current = match;
            }

            return current;
        }
    }
}
