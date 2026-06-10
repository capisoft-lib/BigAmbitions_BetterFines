using UnityEngine;
using UnityEngine.AI;

namespace BetterFines
{
    /// <summary>VoogleRoute-style ground line renderers for traffic-light approach zones.</summary>
    internal sealed class TrafficLightDebugVisualizer : MonoBehaviour
    {
        private const int MaxVisibleZones = 96;
        private const float MaxDrawDistanceM = 180f;
        private const float RefreshSeconds = 0.15f;
        private const int MarkerSegments = 24;
        private const float MarkerRadiusM = 1.4f;
        private const float MarkerLineWidthM = 0.45f;
        private const float DirectionLineLengthM = 7f;
        private const float DirectionLineWidthM = 0.25f;
        private const float DuplicateMarkerMaxDistanceM = 1.5f;
        private const float DuplicateDirectionMinDot = 0.98f;
        private const float GroundYOffset = 0.18f;
        private const float RayStartHeight = 60f;
        private const float RayDistance = 120f;

        private static Material _zoneMaterial;
        private static readonly Color UnknownSignalColor = new Color(0.05f, 0.35f, 1f, 1f);
        private static readonly Color GreenSignalColor = new Color(0.05f, 0.9f, 0.2f, 1f);
        private static readonly Color YellowSignalColor = new Color(1f, 0.78f, 0.05f, 1f);
        private static readonly Color RedSignalColor = new Color(1f, 0.08f, 0.04f, 1f);

        private readonly ZoneView[] _pool = new ZoneView[MaxVisibleZones];
        private float _nextRefreshAt;
        private bool _loggedEmpty;
        private bool _loggedFirstDraw;

        private sealed class ZoneView
        {
            internal GameObject Root;
            internal LineRenderer Outline;
            internal LineRenderer Direction;
            internal LineRenderer LightLine;
        }

        private void Update()
        {
            if (!BetterFinesConfig.ShouldDrawTrafficZones)
            {
                HideAll();
                return;
            }

            if (Time.unscaledTime < _nextRefreshAt)
                return;

            _nextRefreshAt = Time.unscaledTime + RefreshSeconds;

            if (!TrafficDataStore.Stops.IsBuilt)
            {
                LogEmptyOnce("traffic index not built");
                HideAll();
                return;
            }

            var visualLights = TrafficLightVisualIndex.Lights;
            var zones = TrafficDataStore.Stops.Zones;
            if ((visualLights == null || visualLights.Length == 0) &&
                (zones == null || zones.Length == 0))
            {
                LogEmptyOnce("no traffic light visuals or approach zones baked");
                HideAll();
                return;
            }

            EnsureMaterials();
            if (_zoneMaterial == null)
            {
                LogEmptyOnce("line material unavailable");
                HideAll();
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                LogEmptyOnce("main camera unavailable");
                HideAll();
                return;
            }

            if (visualLights != null && visualLights.Length > 0)
                RefreshVisibleLights(visualLights, camera.transform.position);
            else
                RefreshVisibleZones(zones, camera.transform.position);
        }

        private void RefreshVisibleLights(TrafficLightVisualIndex.VisualLight[] lights, Vector3 cameraPosition)
        {
            var maxDistSq = MaxDrawDistanceM * MaxDrawDistanceM;
            var visibleCount = 0;

            for (var i = 0; i < lights.Length && visibleCount < MaxVisibleZones; i++)
            {
                var light = lights[i];
                if (HorizontalDistanceSq(light.Position, cameraPosition) > maxDistSq)
                    continue;

                EnsurePoolItem(visibleCount);
                UpdateLightView(_pool[visibleCount], light);
                visibleCount++;
            }

            for (var i = visibleCount; i < _pool.Length; i++)
            {
                if (_pool[i]?.Root != null)
                    _pool[i].Root.SetActive(false);
            }

            if (!_loggedFirstDraw && visibleCount > 0)
            {
                _loggedFirstDraw = true;
                ModLog.Info("Traffic light visual marker drawing | visible=" + visibleCount +
                            " | total=" + lights.Length);
            }
        }

        private void RefreshVisibleZones(TrafficApproachZone[] zones, Vector3 cameraPosition)
        {
            var maxDistSq = MaxDrawDistanceM * MaxDrawDistanceM;
            var visibleCount = 0;
            var selected = new TrafficApproachZone[MaxVisibleZones];

            for (var i = 0; i < zones.Length && visibleCount < MaxVisibleZones; i++)
            {
                var zone = zones[i];
                if ((zone.WorldBounds.center - cameraPosition).sqrMagnitude > maxDistSq)
                    continue;

                if (IsDuplicateMarker(zone, selected, visibleCount))
                    continue;

                EnsurePoolItem(visibleCount);
                UpdateZoneView(_pool[visibleCount], zone);
                selected[visibleCount] = zone;
                visibleCount++;
            }

            for (var i = visibleCount; i < _pool.Length; i++)
            {
                if (_pool[i]?.Root != null)
                    _pool[i].Root.SetActive(false);
            }

            if (!_loggedFirstDraw && visibleCount > 0)
            {
                _loggedFirstDraw = true;
                ModLog.Info("Traffic light marker debug drawing | visible=" + visibleCount +
                            " | total=" + zones.Length);
            }
        }

        private void EnsurePoolItem(int index)
        {
            if (_pool[index] != null)
                return;

            var root = new GameObject("BetterFines_TrafficLightDebugZone_" + index);
            root.transform.SetParent(transform, false);

            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(root.transform, false);
            var outline = outlineGo.AddComponent<LineRenderer>();
            ConfigureLine(outline, _zoneMaterial, MarkerLineWidthM, true);

            var directionGo = new GameObject("Direction");
            directionGo.transform.SetParent(root.transform, false);
            var direction = directionGo.AddComponent<LineRenderer>();
            ConfigureLine(direction, _zoneMaterial, DirectionLineWidthM, false);

            var lightLineGo = new GameObject("LightLine");
            lightLineGo.transform.SetParent(root.transform, false);
            var lightLine = lightLineGo.AddComponent<LineRenderer>();
            ConfigureLine(lightLine, _zoneMaterial, DirectionLineWidthM, false);

            _pool[index] = new ZoneView
            {
                Root = root,
                Outline = outline,
                Direction = direction,
                LightLine = lightLine
            };
        }

        private static void ConfigureLine(LineRenderer line, Material material, float width, bool loop)
        {
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.loop = loop;
            line.startWidth = width;
            line.endWidth = width;
            line.material = new Material(material)
            {
                name = material.name + " instance",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static void UpdateZoneView(ZoneView view, TrafficApproachZone zone)
        {
            view.Root.SetActive(true);
            HideOutline(view);
            HideLightLine(view);

            var roadForward = zone.RoadForward;
            roadForward.y = 0f;
            if (roadForward.sqrMagnitude < 0.01f)
            {
                view.Direction.positionCount = 0;
                return;
            }

            roadForward.Normalize();
            ApplyColor(view.Direction, GetZoneSignalColor(zone));
            view.Direction.positionCount = 2;
            view.Direction.SetPosition(0, ProjectToGround(zone.StopLinePosition + roadForward * MarkerRadiusM));
            view.Direction.SetPosition(
                1,
                ProjectToGround(zone.StopLinePosition + roadForward * (MarkerRadiusM + DirectionLineLengthM)));
        }

        private static void UpdateLightView(
            ZoneView view,
            TrafficLightVisualIndex.VisualLight light)
        {
            view.Root.SetActive(true);
            HideOutline(view);

            if (!light.HasForward)
            {
                view.Direction.positionCount = 0;
                HideLightLine(view);
                return;
            }

            var color = GetLightSignalColor(light);
            ApplyColor(view.Direction, color);
            view.Direction.positionCount = 2;
            view.Direction.SetPosition(0, ProjectToGround(light.Position + light.Forward * MarkerRadiusM));
            view.Direction.SetPosition(
                1,
                ProjectToGround(light.Position + light.Forward * (MarkerRadiusM + DirectionLineLengthM)));

            UpdateSignalLightLine(view, light, color);
        }

        private static void HideOutline(ZoneView view)
        {
            if (view.Outline != null)
                view.Outline.positionCount = 0;
        }

        private static void HideLightLine(ZoneView view)
        {
            if (view.LightLine != null)
                view.LightLine.positionCount = 0;
        }

        private static void UpdateSignalLightLine(
            ZoneView view,
            TrafficLightVisualIndex.VisualLight light,
            Color color)
        {
            if (!light.TryReadActiveSignal(
                    out _,
                    out var signalForward,
                    out var signalPosition))
            {
                HideLightLine(view);
                return;
            }

            signalForward.y = 0f;
            if (signalForward.sqrMagnitude < 0.01f)
            {
                HideLightLine(view);
                return;
            }

            signalForward.Normalize();
            ApplyColor(view.LightLine, color);
            view.LightLine.positionCount = 2;
            view.LightLine.SetPosition(0, ProjectToGround(signalPosition));
            view.LightLine.SetPosition(1, ProjectToGround(signalPosition + signalForward * DirectionLineLengthM));
        }

        private static Color GetLightSignalColor(TrafficLightVisualIndex.VisualLight light)
        {
            if (light.TryReadActiveSignal(out var signal))
                return SignalToColor(signal);

            return UnknownSignalColor;
        }

        private static Color GetZoneSignalColor(TrafficApproachZone zone)
        {
            if (TrafficDataStore.Stops.TryGetBakedStop(zone.WaypointListIndex, out var baked) &&
                TrafficLightResolver.TryReadSignal(
                    baked.Intersection,
                    baked.IntersectionEntryIndex,
                    out var signal))
                return SignalToColor(signal);

            return UnknownSignalColor;
        }

        private static Color SignalToColor(TrafficApproachSignal signal)
        {
            switch (signal)
            {
                case TrafficApproachSignal.Red:
                    return RedSignalColor;
                case TrafficApproachSignal.Yellow:
                    return YellowSignalColor;
                default:
                    return GreenSignalColor;
            }
        }

        private static bool IsDuplicateMarker(TrafficApproachZone zone, TrafficApproachZone[] selected, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var other = selected[i];
                if (HorizontalDistanceSq(zone.StopLinePosition, other.StopLinePosition) >
                    DuplicateMarkerMaxDistanceM * DuplicateMarkerMaxDistanceM)
                    continue;

                if (DirectionsAligned(zone.RoadForward, other.RoadForward))
                    return true;
            }

            return false;
        }

        private static bool DirectionsAligned(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            if (a.sqrMagnitude < 0.01f || b.sqrMagnitude < 0.01f)
                return true;

            a.Normalize();
            b.Normalize();
            return Vector3.Dot(a, b) >= DuplicateDirectionMinDot;
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static Vector3 ProjectToGround(Vector3 point)
        {
            var lift = Vector3.up * GroundYOffset;
            if (NavMesh.SamplePosition(point, out var navHit, 8f, NavMesh.AllAreas))
                return navHit.position + lift;

            var origin = point + Vector3.up * RayStartHeight;
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out var hit,
                    RayDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                return hit.point + lift;

            point.y += GroundYOffset;
            return point;
        }

        private static void EnsureMaterials()
        {
            if (_zoneMaterial == null)
                _zoneMaterial = CreateMaterial(Color.white, "BetterFines traffic light debug marker");
        }

        private static Material CreateMaterial(Color color, string name)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null)
                return null;

            var material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                color = color
            };
            SetMaterialColor(material, color);
            return material;
        }

        private static void ApplyColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
            if (line.material != null)
                SetMaterialColor(line.material, color);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private void LogEmptyOnce(string reason)
        {
            if (_loggedEmpty)
                return;

            _loggedEmpty = true;
            ModLog.Warn("Traffic zone debug inactive: " + reason +
                        " | debug_traffic_zones=" + BetterFinesConfig.DebugTrafficZones +
                        " | debug_red_light=" + BetterFinesConfig.DebugRedLight);
        }

        private void HideAll()
        {
            for (var i = 0; i < _pool.Length; i++)
            {
                if (_pool[i]?.Root != null)
                    _pool[i].Root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            HideAll();
            DestroyPoolMaterials();

            if (_zoneMaterial != null)
            {
                Destroy(_zoneMaterial);
                _zoneMaterial = null;
            }

        }

        private void DestroyPoolMaterials()
        {
            for (var i = 0; i < _pool.Length; i++)
            {
                DestroyLineMaterial(_pool[i]?.Outline);
                DestroyLineMaterial(_pool[i]?.Direction);
                DestroyLineMaterial(_pool[i]?.LightLine);
            }
        }

        private void DestroyLineMaterial(LineRenderer line)
        {
            if (line != null && line.material != null)
                Destroy(line.material);
        }
    }
}
