using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation.Background
{
    /// <summary>
    /// Cinematic deep-space backdrop: a camera flying forward through streaking stars, layered
    /// nebula sheets, and a handful of drifting planets. Everything is built procedurally at
    /// runtime (no prefabs/hand-authored materials), matching how the rest of the Presentation
    /// layer works (see WorldAnchoredUi, TableView). Content is parented under the camera itself,
    /// so "flying forward" is simulated by animating the content (nebula UV scroll in-shader,
    /// planets drifting in local space) rather than physically moving the camera through a huge
    /// world.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpaceBackgroundView : MonoBehaviour
    {
        const string LayerName = "SpaceBackground";
        const string NebulaShaderName = "LegendsOfTheUniverse/Background/NebulaLayer";
        const string PlanetShaderName = "LegendsOfTheUniverse/Background/PlanetSurface";
        const string RingShaderName = "LegendsOfTheUniverse/Background/PlanetRing";
        const string StarShaderName = "LegendsOfTheUniverse/Background/StarStreak";

        // Shared fake light direction every planet/ring shades against — an approximation of
        // "one distant white core behind the nebula," good enough for a stylized backdrop where
        // every body sits within the same narrow view frustum.
        static readonly Vector3 LightDirection = new Vector3(0.3f, 0.25f, 1f).normalized;

        readonly List<PlanetDrift> planetDrifts = new();

        struct PlanetDrift
        {
            public Transform Transform;
            public Vector3 StartLocalPosition;
            public Vector3 DriftAxis;
            public float Amplitude;
            public float Speed;
            public float Phase;
        }

        /// <summary>Turns an existing camera into the space-flight background camera and builds
        /// its content as children. Safe to call more than once on the same camera (no-ops).
        /// Pass <paramref name="renderDepth"/> to control draw order when composited with another
        /// camera (e.g. -10 so a gameplay camera renders on top of this one).</summary>
        public static SpaceBackgroundView Attach(Camera camera, float? renderDepth = null)
        {
            if (camera == null)
                return null;

            var existing = camera.GetComponent<SpaceBackgroundView>();
            if (existing != null)
                return existing;

            var view = camera.gameObject.AddComponent<SpaceBackgroundView>();
            view.Build(camera, renderDepth);
            return view;
        }

        int layer;

        void Build(Camera camera, float? renderDepth)
        {
            layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
                layer = 0; // Layer not registered yet (project settings not reimported) — fall back to Default.

            camera.orthographic = false;
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.010f, 0.02f, 1f);
            camera.cullingMask = 1 << layer;
            if (renderDepth.HasValue)
                camera.depth = renderDepth.Value;

            var root = camera.transform;
            BuildStars(root);
            BuildNebula(root);
            BuildPlanets(root);
            BuildCoreLight(root);
        }

        void BuildStars(Transform parent)
        {
            var starsObject = new GameObject("Stars") { layer = layer };
            starsObject.transform.SetParent(parent, false);

            var ps = starsObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = 3.5f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.85f, 1f, 1f), Color.white);
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // cone base faces local +Z, tip toward camera.
            shape.position = new Vector3(0f, 0f, 140f);

            var emission = ps.emission;
            emission.rateOverTime = 90f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.z = new ParticleSystem.MinMaxCurve(-45f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 4f;
            renderer.material = CreateMaterial(StarShaderName, "_Tint", Color.white);
        }

        void BuildNebula(Transform parent)
        {
            var layers = new[]
            {
                new NebulaSpec("NebulaIndigo", 44f, new Vector2(70f, 46f), new Color(0.16f, 0.08f, 0.38f), new Color(0.30f, 0.10f, 0.42f), 6f),
                new NebulaSpec("NebulaMagenta", 78f, new Vector2(100f, 66f), new Color(0.30f, 0.09f, 0.40f), new Color(0.46f, 0.12f, 0.36f), -4f),
                new NebulaSpec("NebulaEmber", 120f, new Vector2(150f, 96f), new Color(0.20f, 0.07f, 0.24f), new Color(0.55f, 0.18f, 0.10f), 3f),
            };

            foreach (var spec in layers)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = spec.Name;
                quad.layer = layer;
                var collider = quad.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = false;

                quad.transform.SetParent(parent, false);
                quad.transform.localPosition = new Vector3(0f, 0f, spec.Distance);
                quad.transform.localRotation = Quaternion.Euler(0f, 0f, spec.TiltDegrees);
                quad.transform.localScale = new Vector3(spec.Size.x, spec.Size.y, 1f);

                var material = CreateMaterial(NebulaShaderName);
                material.SetColor("_ColorA", spec.ColorA);
                material.SetColor("_ColorB", spec.ColorB);
                material.SetColor("_EmberColor", new Color(0.85f, 0.28f, 0.10f));
                material.SetFloat("_NoiseScale", 2.2f);
                material.SetFloat("_EmberScale", 6.5f);
                material.SetVector("_ScrollSpeed", new Vector4(0.008f, 0.005f, 0f, 0f));
                material.SetFloat("_Alpha", 0.7f);
                quad.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        readonly struct NebulaSpec
        {
            public readonly string Name;
            public readonly float Distance;
            public readonly Vector2 Size;
            public readonly Color ColorA;
            public readonly Color ColorB;
            public readonly float TiltDegrees;

            public NebulaSpec(string name, float distance, Vector2 size, Color colorA, Color colorB, float tiltDegrees)
            {
                Name = name;
                Distance = distance;
                Size = size;
                ColorA = colorA;
                ColorB = colorB;
                TiltDegrees = tiltDegrees;
            }
        }

        void BuildPlanets(Transform parent)
        {
            // Rust desert world, close on the left — gets the thin red rim (Universe/Remnant accent).
            BuildPlanet(parent, "PlanetRustDesert",
                localPosition: new Vector3(-9f, -1f, 26f),
                diameter: 6.5f,
                colorA: new Color(0.62f, 0.34f, 0.18f),
                colorB: new Color(0.32f, 0.15f, 0.08f),
                rimColor: new Color(1f, 0.30f, 0.18f),
                bandingStrength: 0f,
                driftAxis: new Vector3(-1f, -0.2f, 0f),
                amplitude: 3.5f,
                speed: 0.05f);

            // Ringed gas giant, upper right.
            var gasGiant = BuildPlanet(parent, "PlanetGasGiant",
                localPosition: new Vector3(11f, 9f, 58f),
                diameter: 11f,
                colorA: new Color(0.55f, 0.42f, 0.30f),
                colorB: new Color(0.30f, 0.20f, 0.16f),
                rimColor: new Color(0.95f, 0.92f, 0.85f),
                bandingStrength: 0.85f,
                driftAxis: new Vector3(0.6f, 0.4f, 0f),
                amplitude: 4f,
                speed: 0.04f);
            BuildRing(gasGiant, diameter: 24f);

            // Small ice moon, lower third.
            BuildPlanet(parent, "PlanetIceMoon",
                localPosition: new Vector3(-4f, -7f, 36f),
                diameter: 3.2f,
                colorA: new Color(0.80f, 0.88f, 0.95f),
                colorB: new Color(0.55f, 0.65f, 0.78f),
                rimColor: new Color(0.90f, 0.94f, 1f),
                bandingStrength: 0f,
                driftAxis: new Vector3(-0.5f, -0.3f, 0f),
                amplitude: 2.5f,
                speed: 0.06f);
        }

        Transform BuildPlanet(
            Transform parent,
            string name,
            Vector3 localPosition,
            float diameter,
            Color colorA,
            Color colorB,
            Color rimColor,
            float bandingStrength,
            Vector3 driftAxis,
            float amplitude,
            float speed)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.layer = layer;
            var collider = sphere.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = Vector3.one * diameter;

            var material = CreateMaterial(PlanetShaderName);
            material.SetColor("_ColorA", colorA);
            material.SetColor("_ColorB", colorB);
            material.SetColor("_RimColor", rimColor);
            material.SetFloat("_NoiseScale", 3.2f);
            material.SetFloat("_BandingStrength", bandingStrength);
            material.SetFloat("_Seed", Random.Range(0f, 50f));
            material.SetVector("_LightDir", LightDirection);
            material.SetFloat("_MinLight", 0.14f);
            material.SetFloat("_RimPower", 3.2f);
            material.SetFloat("_RimIntensity", 1.3f);
            sphere.GetComponent<Renderer>().sharedMaterial = material;

            planetDrifts.Add(new PlanetDrift
            {
                Transform = sphere.transform,
                StartLocalPosition = localPosition,
                DriftAxis = driftAxis.normalized,
                Amplitude = amplitude,
                Speed = speed,
                Phase = Random.Range(0f, Mathf.PI * 2f),
            });

            return sphere.transform;
        }

        void BuildRing(Transform planet, float diameter)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "Ring";
            ring.layer = layer;
            var collider = ring.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            ring.transform.SetParent(planet, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.Euler(28f, 0f, 12f);
            ring.transform.localScale = new Vector3(diameter, diameter, 1f) / Mathf.Max(planet.localScale.x, 0.001f);

            var material = CreateMaterial(RingShaderName);
            material.SetColor("_ColorA", new Color(0.68f, 0.58f, 0.46f));
            material.SetColor("_ColorB", new Color(0.36f, 0.29f, 0.22f));
            material.SetFloat("_InnerRadius", 0.20f);
            material.SetFloat("_OuterRadius", 0.47f);
            material.SetVector("_LightSideDir", new Vector4(LightDirection.x, LightDirection.y, 0f, 0f));
            ring.GetComponent<Renderer>().sharedMaterial = material;
        }

        void BuildCoreLight(Transform parent)
        {
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "CoreLight";
            core.layer = layer;
            var collider = core.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            core.transform.SetParent(parent, false);
            core.transform.localPosition = new Vector3(4f, 6f, 190f);
            core.transform.localScale = Vector3.one * 6f;

            // Flat, fully-lit white sphere (reuses the planet shader with shading disabled) —
            // reads as a bright distant point source, not a shaded body.
            var material = CreateMaterial(PlanetShaderName);
            material.SetColor("_ColorA", Color.white);
            material.SetColor("_ColorB", Color.white);
            material.SetColor("_RimColor", Color.white);
            material.SetFloat("_BandingStrength", 0f);
            material.SetFloat("_MinLight", 1f);
            material.SetFloat("_RimIntensity", 0f);
            core.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Material CreateMaterial(string shaderName, string colorProperty = null, Color colorValue = default)
        {
            var shader = Shader.Find(shaderName);
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            if (colorProperty != null)
                material.SetColor(colorProperty, colorValue);
            return material;
        }

        void Update()
        {
            var time = Time.time;
            for (var i = 0; i < planetDrifts.Count; i++)
            {
                var drift = planetDrifts[i];
                if (drift.Transform == null)
                    continue;

                var offset = drift.DriftAxis * (Mathf.Sin(time * drift.Speed + drift.Phase) * drift.Amplitude);
                drift.Transform.localPosition = drift.StartLocalPosition + offset;
            }
        }
    }
}
