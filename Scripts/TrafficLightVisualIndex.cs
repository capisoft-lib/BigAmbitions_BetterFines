using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using GleyTrafficSystem;
using UnityEngine;

namespace BetterFines
{
    /// <summary>One-shot scan of vanilla traffic light scene objects.</summary>
    internal static class TrafficLightVisualIndex
    {
        private const string TrafficLightTag = "TrafficLight";
        private const string DataFolderName = "Data";
        private const string CandidatesCsvName = "traffic_light_visual_candidates.csv";
        private const string GroupsCsvName = "traffic_light_visual_groups.csv";
        private const float DuplicatePositionMaxM = 1.2f;
        private const float DuplicateForwardMinDot = 0.96f;
        private const int MaxReflectionDepth = 4;

        internal readonly struct VisualLight
        {
            internal VisualLight(
                int instanceId,
                string source,
                string name,
                string typeName,
                Vector3 position,
                Vector3 forward,
                bool hasForward,
                GameObject redLight,
                GameObject yellowLight,
                GameObject greenLight)
            {
                InstanceId = instanceId;
                Source = source;
                Name = name;
                TypeName = typeName;
                Position = position;
                Forward = forward;
                HasForward = hasForward;
                RedLight = redLight;
                YellowLight = yellowLight;
                GreenLight = greenLight;
            }

            internal int InstanceId { get; }
            internal string Source { get; }
            internal string Name { get; }
            internal string TypeName { get; }
            internal Vector3 Position { get; }
            internal Vector3 Forward { get; }
            internal bool HasForward { get; }
            internal GameObject RedLight { get; }
            internal GameObject YellowLight { get; }
            internal GameObject GreenLight { get; }
            internal bool HasSignalGroup => RedLight != null || YellowLight != null || GreenLight != null;

            internal bool TryReadActiveSignal(out TrafficApproachSignal signal)
            {
                return TryReadActiveSignal(out signal, out _);
            }

            internal bool TryReadActiveSignal(out TrafficApproachSignal signal, out Vector3 signalForward)
            {
                return TryReadActiveSignal(out signal, out signalForward, out _);
            }

            internal bool TryReadActiveSignal(
                out TrafficApproachSignal signal,
                out Vector3 signalForward,
                out Vector3 signalPosition)
            {
                if (RedLight != null && RedLight.activeInHierarchy)
                {
                    signal = TrafficApproachSignal.Red;
                    signalForward = RedLight.transform.forward;
                    signalPosition = RedLight.transform.position;
                    return true;
                }

                if (YellowLight != null && YellowLight.activeInHierarchy)
                {
                    signal = TrafficApproachSignal.Yellow;
                    signalForward = YellowLight.transform.forward;
                    signalPosition = YellowLight.transform.position;
                    return true;
                }

                if (GreenLight != null && GreenLight.activeInHierarchy)
                {
                    signal = TrafficApproachSignal.None;
                    signalForward = GreenLight.transform.forward;
                    signalPosition = GreenLight.transform.position;
                    return true;
                }

                signal = TrafficApproachSignal.None;
                signalForward = default;
                signalPosition = default;
                return false;
            }
        }

        private static VisualLight[] _lights = Array.Empty<VisualLight>();
        private static string _modRootPath;
        private static bool _loaded;

        internal static VisualLight[] Lights => _lights;
        internal static bool IsLoaded => _loaded;
        internal static int Count => _lights.Length;

        internal static void Initialize(string modRootPath)
        {
            _modRootPath = modRootPath;
        }

        internal static void TryLoadOnce(TrafficStopIndex stops)
        {
            if (_loaded)
                return;

            _loaded = true;
            var result = new List<VisualLight>(256);
            var rawTagged = AddTaggedLights(result);
            var rawNameMatches = AddNamedSceneCandidates(result);
            var rawRendererMatches = AddRendererCandidates(result);
            var rawReflectionMatches = AddReflectionCandidates(stops, result);

            _lights = result.ToArray();
            ExportCandidates();
            ExportGroups();

            ModLog.Info(
                "Traffic light visual index | tagged_raw=" + rawTagged +
                " | name_raw=" + rawNameMatches +
                " | renderer_raw=" + rawRendererMatches +
                " | reflection_raw=" + rawReflectionMatches +
                " | signal_groups=" + CountSignalGroups(_lights) +
                " | unique=" + _lights.Length);

            if (_lights.Length == 0)
                ModLog.Warn("Traffic light visual index found 0 candidates; falling back to stop waypoint geometry.");
        }

        internal static void Invalidate()
        {
            _lights = Array.Empty<VisualLight>();
            _loaded = false;
            _modRootPath = null;
        }

        private static int AddTaggedLights(List<VisualLight> result)
        {
            GameObject[] objects;
            try
            {
                objects = GameObject.FindGameObjectsWithTag(TrafficLightTag);
            }
            catch (UnityException ex)
            {
                ModLog.Warn("Traffic light visual tag scan failed: " + ex.Message);
                return 0;
            }

            if (objects == null || objects.Length == 0)
                return 0;

            for (var i = 0; i < objects.Length; i++)
                TryAddGameObject(result, objects[i], "tag:" + TrafficLightTag, force: true);

            return objects.Length;
        }

        private static int AddNamedSceneCandidates(List<VisualLight> result)
        {
            var raw = 0;
            var transforms = Resources.FindObjectsOfTypeAll<Transform>();
            if (transforms == null)
                return raw;

            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || !IsSceneObject(transform.gameObject))
                    continue;

                var path = GetHierarchyPath(transform);
                if (!LooksLikeTrafficPole(path))
                    continue;

                raw++;
                TryAddTransform(result, transform, "scene-name", path, transform.GetType().Name, force: false);
            }

            return raw;
        }

        private static int AddRendererCandidates(List<VisualLight> result)
        {
            var raw = 0;
            var renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            if (renderers == null)
                return raw;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !IsSceneObject(renderer.gameObject))
                    continue;

                var descriptor = BuildRendererDescriptor(renderer);
                if (!LooksLikeTrafficPole(descriptor))
                    continue;

                raw++;
                TryAddTransform(result, renderer.transform, "renderer", descriptor, renderer.GetType().Name, force: false);
            }

            return raw;
        }

        private static int AddReflectionCandidates(TrafficStopIndex stops, List<VisualLight> result)
        {
            if (stops == null || !stops.IsBuilt)
                return 0;

            var raw = 0;
            var seen = new HashSet<int>();
            foreach (var stop in stops.BakedStops)
            {
                raw += AddReflectionCandidates(stop.Intersection, "intersection", result, seen);

                var entry = TryGetStopEntry(stop);
                raw += AddReflectionCandidates(entry, "stop-entry", result, seen);
            }

            return raw;
        }

        private static object TryGetStopEntry(TrafficStopIndex.BakedStop stop)
        {
            try
            {
                var stopWaypoints = stop.Intersection?.stopWaypoints;
                if (stopWaypoints == null ||
                    stop.IntersectionEntryIndex < 0 ||
                    stop.IntersectionEntryIndex >= stopWaypoints.Count)
                    return null;

                return stopWaypoints[stop.IntersectionEntryIndex];
            }
            catch
            {
                return null;
            }
        }

        private static int AddReflectionCandidates(
            object root,
            string source,
            List<VisualLight> result,
            HashSet<int> seen)
        {
            if (root == null)
                return 0;

            var raw = 0;
            var visited = new HashSet<object>();
            TraverseObject(root, source, source, 0, result, seen, visited, ref raw);
            return raw;
        }

        private static void TraverseObject(
            object value,
            string source,
            string path,
            int depth,
            List<VisualLight> result,
            HashSet<int> seen,
            HashSet<object> visited,
            ref int raw)
        {
            if (value == null || depth > MaxReflectionDepth)
                return;

            if (value is string)
                return;

            if (value is GameObject gameObject)
            {
                if (IsSceneObject(gameObject) &&
                    (LooksLikeTrafficPole(path) || LooksLikeTrafficPole(GetHierarchyPath(gameObject.transform))))
                {
                    raw++;
                    TryAddGameObject(result, gameObject, source + ":" + path, force: false);
                }

                return;
            }

            if (value is Component component)
            {
                if (component != null &&
                    IsSceneObject(component.gameObject) &&
                    (LooksLikeTrafficPole(path) || LooksLikeTrafficPole(GetHierarchyPath(component.transform))))
                {
                    raw++;
                    TryAddTransform(
                        result,
                        component.transform,
                        source + ":" + path,
                        GetHierarchyPath(component.transform),
                        component.GetType().Name,
                        force: false);
                }

                return;
            }

            if (value is Transform transform)
            {
                if (transform != null &&
                    IsSceneObject(transform.gameObject) &&
                    (LooksLikeTrafficPole(path) || LooksLikeTrafficPole(GetHierarchyPath(transform))))
                {
                    raw++;
                    TryAddTransform(
                        result,
                        transform,
                        source + ":" + path,
                        GetHierarchyPath(transform),
                        transform.GetType().Name,
                        force: false);
                }

                return;
            }

            var type = value.GetType();
            if (IsLeafType(type))
                return;

            if (!visited.Add(value))
                return;

            if (value is IEnumerable enumerable)
            {
                var itemIndex = 0;
                foreach (var item in enumerable)
                {
                    TraverseObject(
                        item,
                        source,
                        path + "[" + itemIndex + "]",
                        depth + 1,
                        result,
                        seen,
                        visited,
                        ref raw);
                    itemIndex++;
                }

                return;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var field in type.GetFields(flags))
            {
                object fieldValue;
                try
                {
                    fieldValue = field.GetValue(value);
                }
                catch
                {
                    continue;
                }

                TraverseObject(
                    fieldValue,
                    source,
                    path + "." + field.Name,
                    depth + 1,
                    result,
                    seen,
                    visited,
                    ref raw);
            }

            foreach (var property in type.GetProperties(flags))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    continue;

                object propertyValue;
                try
                {
                    propertyValue = property.GetValue(value);
                }
                catch
                {
                    continue;
                }

                TraverseObject(
                    propertyValue,
                    source,
                    path + "." + property.Name,
                    depth + 1,
                    result,
                    seen,
                    visited,
                    ref raw);
            }
        }

        private static bool TryAddGameObject(List<VisualLight> result, GameObject go, string source, bool force)
        {
            if (go == null)
                return false;

            return TryAddTransform(result, go.transform, source, GetHierarchyPath(go.transform), go.GetType().Name, force);
        }

        private static bool TryAddTransform(
            List<VisualLight> result,
            Transform transform,
            string source,
            string name,
            string typeName,
            bool force)
        {
            if (transform == null)
                return false;

            var forward = transform.forward;
            forward.y = 0f;
            var hasForward = forward.sqrMagnitude > 0.01f;
            if (hasForward)
                forward.Normalize();

            var hasSignalGroup = TryFindSignalChildren(
                transform,
                out var redLight,
                out var yellowLight,
                out var greenLight);
            if (!force && !hasSignalGroup)
                return false;

            var light = new VisualLight(
                transform.gameObject.GetInstanceID(),
                source,
                name,
                typeName,
                transform.position,
                forward,
                hasForward,
                redLight,
                yellowLight,
                greenLight);

            if (!force && IsDuplicate(light, result))
                return false;

            result.Add(light);
            return true;
        }

        private static bool IsDuplicate(VisualLight candidate, List<VisualLight> selected)
        {
            for (var i = 0; i < selected.Count; i++)
            {
                var other = selected[i];
                if (HorizontalDistanceSq(candidate.Position, other.Position) >
                    DuplicatePositionMaxM * DuplicatePositionMaxM)
                    continue;

                if (DirectionsAligned(candidate, other))
                    return true;
            }

            return false;
        }

        private static bool DirectionsAligned(VisualLight a, VisualLight b)
        {
            if (!a.HasForward || !b.HasForward)
                return true;

            return Vector3.Dot(a.Forward, b.Forward) >= DuplicateForwardMinDot;
        }

        private static string BuildRendererDescriptor(Renderer renderer)
        {
            var sb = new StringBuilder();
            sb.Append(GetHierarchyPath(renderer.transform));
            sb.Append("|renderer=").Append(renderer.GetType().Name);

            var materials = renderer.sharedMaterials;
            if (materials != null)
            {
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material != null)
                        sb.Append("|mat=").Append(material.name);
                }
            }

            return sb.ToString();
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var sb = new StringBuilder(transform.name);
            var parent = transform.parent;
            while (parent != null)
            {
                sb.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return sb.ToString();
        }

        private static bool LooksLikeTrafficLight(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var lower = text.ToLowerInvariant();
            return lower.Contains("trafficlight") ||
                   lower.Contains("traffic light") ||
                   lower.Contains("traffic_light") ||
                   lower.Contains("stoplight") ||
                   lower.Contains("signal") ||
                   lower.Contains("semaphore") ||
                   lower.Contains("redlight") ||
                   lower.Contains("yellowlight") ||
                   lower.Contains("greenlight") ||
                   lower.Contains("light_red") ||
                   lower.Contains("light_yellow") ||
                   lower.Contains("light_green");
        }

        private static bool LooksLikeTrafficPole(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var lower = text.ToLowerInvariant();
            if (lower.Contains("streetlight") ||
                lower.Contains("street light") ||
                lower.Contains("streetlamplight") ||
                lower.Contains("streetlamp") ||
                lower.Contains("streetnamesigns") ||
                lower.Contains("street name") ||
                lower.Contains("/redlight") ||
                lower.Contains("/greenlight") ||
                lower.Contains("/yellowlight") ||
                lower.Contains("/streetlamplight") ||
                lower.Contains("/streetnamesigns") ||
                lower.EndsWith("/rightlimit") ||
                lower.EndsWith("/namearea") ||
                lower.Contains(" label"))
                return false;

            return lower.Contains("sm_traffic lights") ||
                   lower.Contains("sm_trafficlight_double");
        }

        private static bool TryFindSignalChildren(
            Transform root,
            out GameObject redLight,
            out GameObject yellowLight,
            out GameObject greenLight)
        {
            redLight = null;
            yellowLight = null;
            greenLight = null;
            if (root == null)
                return false;

            FindSignalChildren(root, ref redLight, ref yellowLight, ref greenLight);
            return redLight != null || yellowLight != null || greenLight != null;
        }

        private static void FindSignalChildren(
            Transform transform,
            ref GameObject redLight,
            ref GameObject yellowLight,
            ref GameObject greenLight)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == null)
                    continue;

                var childName = child.name.ToLowerInvariant();
                if (redLight == null && childName.StartsWith("redlight"))
                    redLight = child.gameObject;
                else if (yellowLight == null && childName.StartsWith("yellowlight"))
                    yellowLight = child.gameObject;
                else if (greenLight == null && childName.StartsWith("greenlight"))
                    greenLight = child.gameObject;

                FindSignalChildren(child, ref redLight, ref yellowLight, ref greenLight);
            }
        }

        private static bool LooksLikeLightMember(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var lower = text.ToLowerInvariant();
            return lower.Contains("light") ||
                   lower.Contains("red") ||
                   lower.Contains("yellow") ||
                   lower.Contains("green");
        }

        private static bool IsSceneObject(GameObject go)
        {
            if (go == null)
                return false;

            var scene = go.scene;
            return scene.IsValid() && scene.isLoaded && go.activeInHierarchy;
        }

        private static bool IsLeafType(Type type)
        {
            return type == null ||
                   type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(decimal) ||
                   type == typeof(string) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector4) ||
                   type == typeof(Quaternion) ||
                   type == typeof(Color) ||
                   type == typeof(Rect) ||
                   type == typeof(Bounds);
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static void ExportCandidates()
        {
            if (string.IsNullOrEmpty(_modRootPath))
                return;

            try
            {
                var folder = Path.Combine(_modRootPath, DataFolderName);
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, CandidatesCsvName);
                File.WriteAllText(path, BuildCandidatesCsv(), Encoding.UTF8);
                ModLog.Info("Exported traffic light visual candidates | count=" + _lights.Length +
                            " | path=" + DataFolderName + "/" + CandidatesCsvName);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to export traffic light visual candidates: " + ex.Message);
            }
        }

        private static string BuildCandidatesCsv()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(4096);
            sb.AppendLine("source,instance_id,type,name,x,y,z,forward_x,forward_y,forward_z,has_forward,has_signal_group,active_signal");

            for (var i = 0; i < _lights.Length; i++)
            {
                var light = _lights[i];
                light.TryReadActiveSignal(out var activeSignal);
                AppendCsv(sb, light.Source);
                sb.Append(',');
                sb.Append(light.InstanceId).Append(',');
                AppendCsv(sb, light.TypeName);
                sb.Append(',');
                AppendCsv(sb, light.Name);
                sb.Append(',');
                sb.Append(light.Position.x.ToString(inv)).Append(',');
                sb.Append(light.Position.y.ToString(inv)).Append(',');
                sb.Append(light.Position.z.ToString(inv)).Append(',');
                sb.Append(light.Forward.x.ToString(inv)).Append(',');
                sb.Append(light.Forward.y.ToString(inv)).Append(',');
                sb.Append(light.Forward.z.ToString(inv)).Append(',');
                sb.Append(light.HasForward ? "true" : "false").Append(',');
                sb.Append(light.HasSignalGroup ? "true" : "false").Append(',');
                sb.Append(activeSignal);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void ExportGroups()
        {
            if (string.IsNullOrEmpty(_modRootPath))
                return;

            try
            {
                var folder = Path.Combine(_modRootPath, DataFolderName);
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, GroupsCsvName);
                File.WriteAllText(path, BuildGroupsCsv(), Encoding.UTF8);
                ModLog.Info("Exported traffic light visual groups | count=" + _lights.Length +
                            " | path=" + DataFolderName + "/" + GroupsCsvName);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to export traffic light visual groups: " + ex.Message);
            }
        }

        private static string BuildGroupsCsv()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(4096);
            sb.AppendLine("instance_id,name,x,y,z,forward_x,forward_y,forward_z,active_signal,red_active,yellow_active,green_active,red_name,yellow_name,green_name");

            for (var i = 0; i < _lights.Length; i++)
            {
                var light = _lights[i];
                light.TryReadActiveSignal(out var activeSignal);
                sb.Append(light.InstanceId).Append(',');
                AppendCsv(sb, light.Name);
                sb.Append(',');
                sb.Append(light.Position.x.ToString(inv)).Append(',');
                sb.Append(light.Position.y.ToString(inv)).Append(',');
                sb.Append(light.Position.z.ToString(inv)).Append(',');
                sb.Append(light.Forward.x.ToString(inv)).Append(',');
                sb.Append(light.Forward.y.ToString(inv)).Append(',');
                sb.Append(light.Forward.z.ToString(inv)).Append(',');
                sb.Append(activeSignal).Append(',');
                sb.Append(IsActive(light.RedLight) ? "true" : "false").Append(',');
                sb.Append(IsActive(light.YellowLight) ? "true" : "false").Append(',');
                sb.Append(IsActive(light.GreenLight) ? "true" : "false").Append(',');
                AppendCsv(sb, light.RedLight != null ? light.RedLight.name : string.Empty);
                sb.Append(',');
                AppendCsv(sb, light.YellowLight != null ? light.YellowLight.name : string.Empty);
                sb.Append(',');
                AppendCsv(sb, light.GreenLight != null ? light.GreenLight.name : string.Empty);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static bool IsActive(GameObject gameObject)
        {
            return gameObject != null && gameObject.activeInHierarchy;
        }

        private static int CountSignalGroups(VisualLight[] lights)
        {
            if (lights == null)
                return 0;

            var count = 0;
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i].HasSignalGroup)
                    count++;
            }

            return count;
        }

        private static string BuildComponentList(Component[] components)
        {
            if (components == null || components.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (var i = 0; i < components.Length; i++)
            {
                if (i > 0)
                    sb.Append('|');
                sb.Append(components[i] != null ? components[i].GetType().FullName : "<null>");
            }

            return sb.ToString();
        }

        private static string BuildRendererMaterialList(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                var materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;

                for (var j = 0; j < materials.Length; j++)
                {
                    if (sb.Length > 0)
                        sb.Append('|');
                    var material = materials[j];
                    if (material == null)
                    {
                        sb.Append("<null>");
                        continue;
                    }

                    sb.Append(material.name);
                    if (material.shader != null)
                        sb.Append('@').Append(material.shader.name);
                }
            }

            return sb.ToString();
        }


        private static void AppendCsv(StringBuilder sb, string value)
        {
            value = value ?? string.Empty;
            sb.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch == '"')
                    sb.Append("\"\"");
                else
                    sb.Append(ch);
            }

            sb.Append('"');
        }

    }
}
