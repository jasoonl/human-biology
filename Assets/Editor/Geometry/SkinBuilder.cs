using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The body surface: one continuous shell, the smooth union of a head, neck, torso,
    /// arms and legs, meshed once. It replaces nineteen separate overlapping ellipsoids.
    /// Dimensions are the anthropometric ones the earlier skin used, which the organs
    /// were fitted inside, so the interior still fits.
    ///
    /// The field is kept after building so <see cref="ContainmentReport"/> can test every
    /// internal structure's vertices against it and name whatever pokes through.
    /// </summary>
    public static class SkinBuilder
    {
        public const string SkinFemaleName = "SkinFemale";
        public static SdfFunc Field { get; private set; }

        private static SdfFunc Limb(BodyShape.Limb l) => Sdf.RoundCone(l.A, l.RA, l.B, l.RB);

        private static SdfFunc BuildField(bool female = false)
        {
            // Head, with the features that give the silhouette a face and ears.
            SdfFunc skull = Sdf.SmoothUnion(0.016f,
                Sdf.Ellipsoid(new Vector3(0f, 1.635f, 0.005f), new Vector3(0.0825f, 0.1175f, 0.105f)),
                // Skin follows the brow and the cheekbones; a plain ellipsoid leaves both
                // poking out of it, because the face is fuller than the back of the head.
                Sdf.Ellipsoid(new Vector3(0f, 1.678f, -0.062f), new Vector3(0.072f, 0.040f, 0.038f)),
                Sdf.Ellipsoid(new Vector3(0f, 1.612f, -0.052f), new Vector3(0.060f, 0.062f, 0.052f)),
                Sdf.Capsule(new Vector3(0f, 1.652f, -0.094f), new Vector3(0f, 1.616f, -0.101f), 0.0085f),  // nose
                Sdf.Sphere(new Vector3(0f, 1.616f, -0.104f), 0.0105f),                                     // tip of nose
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.083f, 1.640f, 0.015f), new Vector3(0.0085f, 0.028f, 0.017f))), // ears
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.036f, 1.650f, -0.070f), new Vector3(0.038f, 0.030f, 0.030f))), // eye sockets
                Sdf.Ellipsoid(new Vector3(0f, 1.558f, -0.050f), new Vector3(0.058f, 0.045f, 0.052f)),     // jaw
                Sdf.Ellipsoid(new Vector3(0f, 1.540f, -0.074f), new Vector3(0.026f, 0.025f, 0.027f)));   // chin

            // Soft features laid on the skull with a tight blend so they read as separate forms.
            // They only add volume, so nothing inside can end up outside the skin.
            SdfFunc features = Sdf.SmoothUnion(0.005f,
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.033f, 1.652f, -0.085f), new Vector3(0.016f, 0.012f, 0.014f))),   // eyeballs and lids
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.047f, 1.624f, -0.076f), new Vector3(0.024f, 0.017f, 0.020f))),   // cheekbones
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.019f, 1.606f, -0.094f), new Vector3(0.011f, 0.009f, 0.010f))),   // nostril wings
                Sdf.Ellipsoid(new Vector3(0f, 1.585f, -0.098f), new Vector3(0.026f, 0.0065f, 0.011f)),                   // upper lip
                Sdf.Ellipsoid(new Vector3(0f, 1.572f, -0.097f), new Vector3(0.023f, 0.0075f, 0.012f)),                   // lower lip
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.087f, 1.638f, 0.017f), new Vector3(0.0075f, 0.031f, 0.021f))),   // outer ear rims
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0865f, 1.612f, 0.017f), new Vector3(0.0085f, 0.010f, 0.011f)))); // earlobes
            SdfFunc head = Sdf.SmoothUnion(0.006f, skull, features);

            SdfFunc neck = Sdf.Capsule(new Vector3(0f, 1.565f, 0.026f), new Vector3(0f, 1.440f, 0.028f), 0.062f);

            // Torso: chest, abdomen and pelvis blended into one, with the buttocks behind.
            // Rounded boxes rather than ellipsoids: a chest is nearly as deep at the sides
            // of the back as at the middle, but an ellipsoid narrows fast away from the
            // midline and leaves the shoulder blades and spine poking out of it.
            SdfFunc torso = Sdf.SmoothUnion(0.11f,
                Sdf.RoundBox(new Vector3(0f, 1.285f, 0.005f), new Vector3(0.180f, 0.205f, 0.132f), 0.090f, Quaternion.identity),
                Sdf.RoundBox(new Vector3(0f, 1.060f, -0.003f), new Vector3(0.166f, 0.175f, 0.118f), 0.090f, Quaternion.identity),
                Sdf.RoundBox(new Vector3(0f, 0.900f, 0.000f), new Vector3(0.178f, 0.135f, 0.128f), 0.085f, Quaternion.identity),
                // One column enclosing the three sections, so their slightly different widths do not
                // show up as horizontal ridges across the trunk.
                Sdf.RoundBox(new Vector3(0f, 1.128f, 0.002f), new Vector3(0.180f, 0.363f, 0.130f), 0.090f, Quaternion.identity),
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.085f, 0.895f, 0.060f), new Vector3(0.085f, 0.085f, 0.070f))),
                // The slope of the trapezius from neck to shoulder.
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.078f, 1.462f, 0.046f), new Vector3(0.078f, 0.052f, 0.068f))));

            // Shoulders, arms and hands.
            SdfFunc arm = Sdf.SmoothUnion(0.03f,
                Sdf.Ellipsoid(new Vector3(0.19f, 1.39f, -0.01f), new Vector3(0.0725f, 0.0825f, 0.0725f)),
                Limb(BodyShape.UpperArm),
                Limb(BodyShape.Forearm),
                Sdf.Ellipsoid(new Vector3(0.205f, 0.735f, -0.004f), new Vector3(0.0425f, 0.102f, 0.030f)),
                Sdf.Chain(new[] { new Vector3(0.228f, 0.815f, 0.000f), new Vector3(0.250f, 0.750f, -0.028f), new Vector3(0.264f, 0.722f, -0.048f) },
                    new[] { 0.0175f, 0.0155f, 0.0135f }, 0.01f),   // thumb, curling forward to its tip
                // The four fingers, knuckle to tip, curled slightly toward the palm.
                Sdf.Capsule(new Vector3(0.233f, 0.742f, 0.008f), new Vector3(0.246f, 0.655f, -0.020f), 0.0115f),
                Sdf.Capsule(new Vector3(0.216f, 0.742f, 0.008f), new Vector3(0.221f, 0.646f, -0.022f), 0.0115f),
                Sdf.Capsule(new Vector3(0.199f, 0.749f, 0.008f), new Vector3(0.196f, 0.658f, -0.020f), 0.0110f),
                Sdf.Capsule(new Vector3(0.183f, 0.755f, 0.008f), new Vector3(0.176f, 0.676f, -0.016f), 0.0100f));

            // Thighs, calves and feet.
            SdfFunc leg = Sdf.SmoothUnion(0.03f,
                Limb(BodyShape.Thigh),
                Limb(BodyShape.Shank),
                Sdf.Ellipsoid(new Vector3(0.085f, 0.32f, 0.030f), new Vector3(0.052f, 0.110f, 0.058f)),        // calf
                Sdf.Ellipsoid(new Vector3(0.085f, 0.044f, -0.066f), new Vector3(0.0665f, 0.050f, 0.1600f)),      // foot
                Sdf.Ellipsoid(new Vector3(0.084f, 0.028f, -0.168f), new Vector3(0.050f, 0.028f, 0.048f)));     // toes

            // The figure's exterior is male, so the skin encloses the scrotum and the penis, which the male
            // reproductive organs sit in. (The female organs, shown instead by the layer's sex switch, are internal.)
            SdfFunc genitals = Sdf.SmoothUnion(0.012f,
                Sdf.Ellipsoid(new Vector3(0f, 0.756f, -0.040f), new Vector3(0.040f, 0.040f, 0.033f)),
                Sdf.RoundCone(new Vector3(0f, 0.792f, -0.056f), 0.0165f, new Vector3(0f, 0.734f, -0.092f), 0.0140f));

            // The female exterior has no external genitals (the vulva is not modelled) and a fuller chest.
            SdfFunc breasts = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.078f, 1.255f, -0.085f), new Vector3(0.056f, 0.056f, 0.040f)));

            return female
                ? Sdf.SmoothUnion(0.035f, head, neck, torso, Sdf.MirrorX(arm), Sdf.MirrorX(leg), breasts)
                : Sdf.SmoothUnion(0.035f, head, neck, torso, Sdf.MirrorX(arm), Sdf.MirrorX(leg), genitals);
        }

        public static void Build(Transform root, Material skin, int layer)
        {
            Field = BuildField();
            var min = new Vector3(-0.32f, -0.02f, -0.26f);
            var max = new Vector3(0.32f, 1.78f, 0.20f);
            Mesh mesh = PartFactory.Save(SurfaceNets.Build(Field, min, max, 0.007f, "SkinShell", 2f), "SkinShell");
            PartFactory.Add(root, "Skin", "SYS_INTEG_SKIN", mesh, skin, layer);

            // The same skin with the female exterior; AnatomyLayerVisibility shows whichever matches the
            // reproductive sex switch. Both carry the one SYS_INTEG_SKIN id so the dictionary entry is shared.
            Mesh female = PartFactory.Save(SurfaceNets.Build(BuildField(true), min, max, 0.007f, "SkinShellFemale", 2f), "SkinShellFemale");
            PartFactory.Add(root, SkinFemaleName, "SYS_INTEG_SKIN", female, skin, layer);
        }

        /// <summary>
        /// For every mesh under <paramref name="root"/> other than the skin, how far its
        /// most outlying vertex sits beyond the skin surface. Positive means poking out.
        /// Returns the worst offenders so a misplaced structure is named, not just detected.
        /// </summary>
        public static List<(string name, float outside)> ContainmentReport(Transform root, float tolerance = 0.006f)
        {
            var result = new List<(string, float)>();
            if (Field == null) Field = BuildField();

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || filter.name == "Skin" || filter.name == SkinFemaleName) continue;

                float worst = float.MinValue;
                Vector3 where = Vector3.zero;
                Transform t = filter.transform;
                foreach (Vector3 v in filter.sharedMesh.vertices)
                {
                    Vector3 p = root.InverseTransformPoint(t.TransformPoint(v));
                    float d = Field(p);
                    if (d > worst) { worst = d; where = p; }
                }

                if (worst > tolerance) result.Add(($"{filter.name} @({where.x:F2}, {where.y:F2}, {where.z:F2})", worst));
            }

            result.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return result;
        }
    }
}
