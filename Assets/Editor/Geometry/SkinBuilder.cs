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
                Sdf.Ellipsoid(new Vector3(0f, 1.622f, -0.085f), new Vector3(0.0080f, 0.016f, 0.0095f)),         // nasal bridge
                Sdf.Sphere(new Vector3(0f, 1.612f, -0.107f), 0.0115f),                                    // bulbous tip
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.083f, 1.640f, 0.015f), new Vector3(0.0085f, 0.028f, 0.017f))), // ears
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.037f, 1.648f, -0.058f), new Vector3(0.038f, 0.036f, 0.032f))),           // eye sockets (deeper)
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.037f, 1.660f, -0.053f), new Vector3(0.033f, 0.0058f, 0.028f))),           // supraorbital ridge
                Sdf.Ellipsoid(new Vector3(0f, 1.556f, -0.048f), new Vector3(0.068f, 0.052f, 0.058f)),     // jaw base
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.055f, 1.544f, -0.058f), new Vector3(0.020f, 0.035f, 0.038f))), // jaw angle
                Sdf.Ellipsoid(new Vector3(0f, 1.540f, -0.074f), new Vector3(0.026f, 0.025f, 0.027f)));   // chin

            // Soft features laid on the skull with a tight blend so they read as separate forms.
            // They only add volume, so nothing inside can end up outside the skin.
            SdfFunc features = Sdf.SmoothUnion(0.005f,
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.033f, 1.652f, -0.085f), new Vector3(0.016f, 0.012f, 0.014f))),   // eyeballs and lids
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.047f, 1.624f, -0.076f), new Vector3(0.024f, 0.017f, 0.020f))),   // cheekbones
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.019f, 1.606f, -0.094f), new Vector3(0.011f, 0.009f, 0.010f))),   // nostril wings
                Sdf.Ellipsoid(new Vector3(0f, 1.585f, -0.094f), new Vector3(0.0248f, 0.0070f, 0.0110f)),               // upper lip
                Sdf.Ellipsoid(new Vector3(0f, 1.572f, -0.092f), new Vector3(0.0218f, 0.0082f, 0.0115f)),                 // lower lip
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.087f, 1.638f, 0.017f), new Vector3(0.0075f, 0.031f, 0.021f))),   // outer ear rims
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0865f, 1.612f, 0.017f), new Vector3(0.0085f, 0.010f, 0.011f)))); // earlobes
            // Refined face: stronger zygomatic arches, temporal hollows, defined eye sockets, buccinator
            SdfFunc cheekbones = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.046f, 1.613f, -0.046f), new Vector3(0.026f, 0.020f, 0.023f))); // sharp zygomatic
            SdfFunc temporal = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.064f, 1.652f, 0.022f), new Vector3(0.026f, 0.042f, 0.040f))); // temporalis
            SdfFunc head = Sdf.SmoothUnion(0.010f, skull, features, cheekbones, temporal);

            SdfFunc neck = Sdf.Capsule(new Vector3(0f, 1.568f, 0.027f), new Vector3(0f, 1.438f, 0.030f), 0.065f);

            // Torso: chest, abdomen and pelvis blended into one, with the buttocks behind.
            // Rounded boxes rather than ellipsoids: a chest is nearly as deep at the sides
            // of the back as at the middle, but an ellipsoid narrows fast away from the
            // midline and leaves the shoulder blades and spine poking out of it.
            SdfFunc torso = Sdf.SmoothUnion(0.13f,
                Sdf.RoundBox(new Vector3(0f, 1.305f, 0.010f), new Vector3(0.188f, 0.215f, 0.138f), 0.098f, Quaternion.identity),  // chest
                Sdf.RoundBox(new Vector3(0f, 1.070f, 0.002f), new Vector3(0.170f, 0.185f, 0.122f), 0.094f, Quaternion.identity),  // abdomen
                Sdf.RoundBox(new Vector3(0f, 0.900f, 0.004f), new Vector3(0.172f, 0.145f, 0.128f), 0.090f, Quaternion.identity),  // pelvis
                Sdf.RoundBox(new Vector3(0f, 1.128f, 0.005f), new Vector3(0.184f, 0.373f, 0.135f), 0.094f, Quaternion.identity),  // envelope
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.085f, 0.895f, 0.060f), new Vector3(0.085f, 0.085f, 0.070f))),
                // The slope of the trapezius from neck to shoulder.
                Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.086f, 1.458f, 0.050f), new Vector3(0.085f, 0.058f, 0.072f))));  // trapezius

            // Shoulders, arms and hands.
            SdfFunc arm = Sdf.SmoothUnion(0.033f,
                Sdf.Ellipsoid(new Vector3(0.195f, 1.398f, -0.006f), new Vector3(0.0780f, 0.0870f, 0.0780f)),  // shoulder
                Limb(BodyShape.UpperArm),
                Limb(BodyShape.Forearm),
                Sdf.Ellipsoid(new Vector3(0.210f, 0.740f, 0.000f), new Vector3(0.0460f, 0.107f, 0.033f)),     // wrist
                Sdf.Chain(new[] { new Vector3(0.228f, 0.815f, 0.000f), new Vector3(0.250f, 0.750f, -0.028f), new Vector3(0.264f, 0.722f, -0.048f) },
                    new[] { 0.0175f, 0.0155f, 0.0135f }, 0.01f),   // thumb, curling forward to its tip
                // The four fingers, knuckle to tip, curled slightly toward the palm.
                Sdf.Capsule(new Vector3(0.233f, 0.742f, 0.008f), new Vector3(0.246f, 0.655f, -0.020f), 0.0115f),
                Sdf.Capsule(new Vector3(0.216f, 0.742f, 0.008f), new Vector3(0.221f, 0.646f, -0.022f), 0.0115f),
                Sdf.Capsule(new Vector3(0.199f, 0.749f, 0.008f), new Vector3(0.196f, 0.658f, -0.020f), 0.0110f),
                Sdf.Capsule(new Vector3(0.183f, 0.755f, 0.008f), new Vector3(0.176f, 0.676f, -0.016f), 0.0100f));

            // Thighs, calves and feet.
            SdfFunc leg = Sdf.SmoothUnion(0.034f,
                Limb(BodyShape.Thigh),
                Limb(BodyShape.Shank),
                Sdf.Ellipsoid(new Vector3(0.085f, 0.320f, 0.034f), new Vector3(0.056f, 0.115f, 0.062f)),        // calf
                Sdf.Ellipsoid(new Vector3(0.085f, 0.043f, -0.064f), new Vector3(0.0700f, 0.054f, 0.1640f)),      // foot
                Sdf.Ellipsoid(new Vector3(0.084f, 0.028f, -0.168f), new Vector3(0.053f, 0.031f, 0.051f)));      // toes

            // The figure's exterior is male, so the skin encloses the scrotum and the penis, which the male
            // reproductive organs sit in. (The female organs, shown instead by the layer's sex switch, are internal.)
            SdfFunc genitals = Sdf.SmoothUnion(0.013f,
                Sdf.Ellipsoid(new Vector3(0f, 0.754f, -0.038f), new Vector3(0.038f, 0.038f, 0.031f)),  // scrotum
                Sdf.RoundCone(new Vector3(0f, 0.790f, -0.055f), 0.0160f, new Vector3(0f, 0.732f, -0.090f), 0.0135f));  // penis

            // The female exterior has no external genitals (the vulva is not modelled) and a fuller chest.
            SdfFunc breasts = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.078f, 1.255f, -0.085f), new Vector3(0.056f, 0.056f, 0.040f)));

            return female
                ? Sdf.SmoothUnion(0.035f, head, neck, torso, Sdf.MirrorX(arm), Sdf.MirrorX(leg), breasts)
                : Sdf.SmoothUnion(0.035f, head, neck, torso, Sdf.MirrorX(arm), Sdf.MirrorX(leg), genitals);
        }


        // ------------------------------------------------------------ features the atlas leaves out

        /// <summary>Distance along a ray, from <paramref name="origin"/>, to where the field first goes negative
        /// (into the body); negative when it never does.</summary>
        private static float Surface(SdfFunc f, Vector3 origin, Vector3 dir, float max)
        {
            float prev = 0f;
            for (float t = 0f; t <= max; t += 0.002f)
            {
                if (f(origin + dir * t) < 0f)
                {
                    // bisect between the last outside sample and this one
                    float lo = prev, hi = t;
                    for (int i = 0; i < 12; i++)
                    {
                        float mid = (lo + hi) * 0.5f;
                        if (f(origin + dir * mid) < 0f) hi = mid; else lo = mid;
                    }
                    return hi;
                }
                prev = t;
            }
            return -1f;
        }

        /// <summary>Eyes and lips: the atlas' skin fit smooths both away, so put them back where the eyeballs and
        /// the mouth are.</summary>
        private static SdfFunc WithFace(SdfFunc body)
        {
            // Eyeball centres, from the atlas, in figure space.
            var eye = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(EyeX, EyeY, EyeZ), new Vector3(0.0158f, 0.0138f, 0.0140f)));
            var lidCrease = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(EyeX, EyeY + 0.0125f, EyeZ - 0.0005f), new Vector3(0.0170f, 0.0060f, 0.0140f)));

            // Brow ridges, sitting on the bone just above each eye.
            float bz = Surface(body, new Vector3(0.034f, 1.647f, -0.30f), Vector3.forward, 0.4f);
            if (bz > 0f)
                eye = Sdf.SmoothUnion(0.006f, eye, Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.033f, 1.647f, -0.30f + bz - 0.002f), new Vector3(0.017f, 0.0065f, 0.0085f), Quaternion.Euler(0f, -22f, -5f))));

            float z = Surface(body, new Vector3(0f, 1.560f, -0.30f), Vector3.forward, 0.4f);
            if (z < 0f) return Sdf.SmoothUnion(0.004f, body, eye, lidCrease);
            float lipZ = -0.30f + z;

            var upper = Sdf.Ellipsoid(new Vector3(0f, 1.5685f, lipZ + 0.0075f), new Vector3(0.0255f, 0.0078f, 0.0105f));
            var lower = Sdf.Ellipsoid(new Vector3(0f, 1.5535f, lipZ + 0.0085f), new Vector3(0.0215f, 0.0085f, 0.0105f));
            SdfFunc faced = Sdf.SmoothUnion(0.007f, body, eye, lidCrease, upper, lower);
            // The line where the lips meet: a shallow crease, not a slot.
            var seam = Sdf.Ellipsoid(new Vector3(0f, 1.5598f, lipZ - 0.0030f), new Vector3(0.0220f, 0.00085f, 0.0050f));
            var lipLine = Sdf.Ellipsoid(new Vector3(0f, 1.5780f, lipZ - 0.0048f), new Vector3(0.0225f, 0.00065f, 0.0035f));
            return Sdf.Subtract(faced, Sdf.SmoothUnion(0.0008f, seam, lipLine));
        }


        /// <summary>Features that differ by sex, all additive so nothing inside can end up outside the skin: a male
        /// face gets a heavier brow, squarer jaw and chin, a larger nose and an Adam's apple; a female face gets fuller
        /// cheeks and lips and a softer, more pointed chin.</summary>
        private static SdfFunc WithFaceSex(SdfFunc body, bool female)
        {
            float bz = Surface(body, new Vector3(0.030f, 1.660f, -0.30f), Vector3.forward, 0.4f);
            float nz = Surface(body, new Vector3(0f, 1.628f, -0.30f), Vector3.forward, 0.4f);
            float lz = Surface(body, new Vector3(0f, 1.5685f, -0.30f), Vector3.forward, 0.4f);
            var parts = new List<SdfFunc> { body };
            if (!female)
            {
                if (bz > 0f) parts.Add(Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.030f, 1.658f, -0.30f + bz), new Vector3(0.018f, 0.0045f, 0.0055f), Quaternion.Euler(0f, -12f, -4f))));
                if (nz > 0f) parts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.628f, -0.30f + nz + 0.001f), new Vector3(0.0085f, 0.022f, 0.0085f)));
                parts.Add(Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.049f, 1.558f, -0.010f), new Vector3(0.008f, 0.020f, 0.022f))));   // jaw angles
                parts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.541f, -0.080f), new Vector3(0.026f, 0.014f, 0.012f)));                    // squarer chin
                float az = Surface(body, new Vector3(0f, 1.515f, -0.30f), Vector3.forward, 0.4f);
                if (az > 0f) parts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.515f, -0.30f + az + 0.001f), new Vector3(0.0075f, 0.013f, 0.0085f)));   // Adam's apple
            }
            else
            {
                if (lz > 0f)
                {
                    parts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.5690f, -0.30f + lz + 0.0015f), new Vector3(0.0240f, 0.0058f, 0.0050f)));   // fuller lips
                    parts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.5535f, -0.30f + lz + 0.0020f), new Vector3(0.0205f, 0.0068f, 0.0055f)));
                }
                parts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.538f, -0.080f), new Vector3(0.016f, 0.016f, 0.012f)));                    // small pointed chin
            }
            return Sdf.SmoothUnion(0.008f, parts.ToArray());
        }

        /// <summary>The nostrils and the groove between nose and upper lip, cut into the finished skin.</summary>
        private static SdfFunc WithNostrilsAndMouth(SdfFunc body, bool female)
        {
            float nz = Surface(body, new Vector3(0f, 1.604f, -0.30f), Vector3.forward, 0.4f);
            float lz = Surface(body, new Vector3(0f, 1.580f, -0.30f), Vector3.forward, 0.4f);
            var cuts = new List<SdfFunc>();
            if (nz > 0f)
            {
                float z = -0.30f + nz;
                cuts.Add(Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0095f, 1.6015f, z + 0.0015f), new Vector3(0.0052f, 0.0042f, 0.0095f), Quaternion.Euler(-38f, 0f, -6f))));
            }
            if (lz > 0f)
            {
                float z = -0.30f + lz;
                cuts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.5775f, z + 0.0022f), new Vector3(0.0032f, 0.0050f, 0.0012f)));   // philtrum
                // mouth corners
                cuts.Add(Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0245f, 1.5595f, z + 0.0058f), new Vector3(0.0028f, 0.0040f, 0.0042f))));
            }
            float cz = Surface(body, new Vector3(0f, 1.538f, -0.30f), Vector3.forward, 0.4f);
            if (cz > 0f)
                cuts.Add(Sdf.Ellipsoid(new Vector3(0f, 1.535f, -0.30f + cz + 0.0008f), new Vector3(0.0045f, 0.0035f, 0.0020f)));
            if (cuts.Count == 0) return body;
            return Sdf.Subtract(body, Sdf.SmoothUnion(0.002f, cuts.ToArray()));
        }

        // ---------------------------------------------------------------- eyes

        // The eyeball centre (the atlas' sclera, in figure space) and radius.
        private const float EyeX = 0.0319f, EyeY = 1.627f, EyeZ = -0.0690f, EyeR = 0.0135f;

        /// <summary>The opening between the lids: an almond-shaped prism cut through the eye bulge, its outer corner a
        /// little higher, down to the surface of the globe, so the eyeball fills it without a pit or a gap.</summary>
        private static SdfFunc WithFissures(SdfFunc body)
        {
            var almond = Sdf.Ellipsoid(new Vector3(EyeX, EyeY + 0.0005f, EyeZ), new Vector3(0.0130f, 0.0056f, 0.060f), Quaternion.Euler(0f, 0f, 4f));
            SdfFunc inFront = p => p.z - EyeZ;                                      // negative in front of the globe's centre plane
            var globe = Sdf.Sphere(new Vector3(EyeX, EyeY, EyeZ), EyeR - 0.0006f);
            var fissure = Sdf.MirrorX(Sdf.Subtract(Sdf.Intersect(almond, inFront), globe));
            return Sdf.Subtract(body, fissure);
        }

        /// <summary>Eyeballs (sclera, iris, pupil, painted as vertex colour with alpha 0 so the shader leaves the skin
        /// tone off them) and eyelashes, to be merged into the skin mesh.</summary>
        private static Mesh EyeParts(SdfFunc skin)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color>();
            var tris = new List<int>();

            for (int side = -1; side <= 1; side += 2)
            {
                var centre = new Vector3(side * EyeX, EyeY, EyeZ);
                // Gaze straight ahead, a hair outward.
                Vector3 front = Quaternion.Euler(0f, side * 2f, 0f) * Vector3.back;
                Vector3 up = Vector3.up;
                Vector3 right = Vector3.Cross(up, front).normalized;
                up = Vector3.Cross(front, right).normalized;
                Vector3 inner = new Vector3(-side, 0f, 0f);

                const int rings = 90, segs = 96;
                int baseIndex = verts.Count;
                for (int r = 0; r <= rings; r++)
                {
                    float polar = Mathf.PI * r / rings;
                    float deg = polar * Mathf.Rad2Deg;
                    for (int k = 0; k <= segs; k++)
                    {
                        float phi = 2f * Mathf.PI * k / segs;
                        Vector3 d = front * Mathf.Cos(polar) + (right * Mathf.Cos(phi) + up * Mathf.Sin(phi)) * Mathf.Sin(polar);
                        // The cornea bulges a millimetre in front of the sclera.
                        float bulge = 1f + 0.085f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 34f, deg)));
                        verts.Add(centre + d * (EyeR * bulge));
                        normals.Add(d);
                        colours.Add(EyeColour(d, deg, phi, inner));
                    }
                }
                for (int r = 0; r < rings; r++)
                    for (int k = 0; k < segs; k++)
                    {
                        int a = baseIndex + r * (segs + 1) + k, b = a + segs + 1;
                        tris.Add(a); tris.Add(b); tris.Add(a + 1);
                        tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                    }

                // Eyelashes along the lid margins: thin curved blades, upper ones long and curling up.
                var lash = new Color(0.07f, 0.05f, 0.04f, 0f);
                for (int pass = 0; pass < 2; pass++)
                {
                    int count = pass == 0 ? 13 : 7;
                    for (int i = 0; i < count; i++)
                    {
                        float u = (i + 0.5f) / count;
                        float xr = Mathf.Lerp(-0.0118f, 0.0125f, u);                       // inner to outer corner
                        float shape = Mathf.Sqrt(Mathf.Max(0f, 1f - (xr / 0.0132f) * (xr / 0.0132f)));
                        float yr = (pass == 0 ? 1f : -0.9f) * 0.0054f * shape + xr * 0.07f;
                        // Root each lash on the lid's actual surface, just beyond the edge of the opening.
                        float rx = side * EyeX + side * xr, ry = EyeY + 0.0005f + yr + (pass == 0 ? 0.0007f : -0.0007f);
                        float sz = Surface(skin, new Vector3(rx, ry, -0.30f), Vector3.forward, 0.4f);
                        var root = new Vector3(rx, ry - (pass == 0 ? 0.0007f : -0.0007f), sz > 0f ? -0.30f + sz - 0.0004f : EyeZ - 0.0132f);
                        float len = (pass == 0 ? 0.0088f : 0.0042f) * Mathf.Lerp(0.62f, 1f, Mathf.SmoothStep(0f, 1f, u * 1.4f)) * (0.75f + 0.5f * Mathf.Abs(Mathf.Sin(i * 12.9898f)));
                        Vector3 dir = new Vector3(side * 0.18f * (u - 0.35f), pass == 0 ? 0.55f : -0.55f, -0.82f).normalized;
                        Vector3 curl = new Vector3(0f, pass == 0 ? 1f : -1f, 0.15f);   // the tip turns up (or down)
                        int b0 = verts.Count;
                        const int segsL = 4;
                        Vector3 prev = root;
                        for (int s = 0; s <= segsL; s++)
                        {
                            float t = (float)s / segsL;
                            Vector3 pos = root + dir * (len * t) + curl * (len * 0.55f * t * t);
                            float w = 0.00026f * (1f - t * 0.8f);
                            Vector3 side3 = new Vector3(1f, 0f, 0f) * w;
                            verts.Add(pos - side3); verts.Add(pos + side3);
                            Vector3 nrm = new Vector3(0f, 0.25f * (pass == 0 ? 1f : -1f), -0.97f).normalized;
                            normals.Add(nrm); normals.Add(nrm);
                            colours.Add(lash); colours.Add(lash);
                        }
                        for (int s = 0; s < segsL; s++)
                        {
                            int a = b0 + s * 2;
                            tris.Add(a); tris.Add(a + 2); tris.Add(a + 1);
                            tris.Add(a + 1); tris.Add(a + 2); tris.Add(a + 3);
                            tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);      // second winding: visible from both sides
                            tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
                        }
                    }
                }
            }

            var mesh = new Mesh { name = "EyeParts", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colours);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Colour of the eyeball at angle <paramref name="deg"/> from the gaze axis: pupil, amber-brown iris
        /// with a dark limbal ring, then white sclera going faintly pink toward the corners.</summary>
        private static Color EyeColour(Vector3 d, float deg, float phi, Vector3 inner)
        {
            float ring = 0.9f + 0.1f * Mathf.Sin(phi * 23f) * Mathf.Sin(phi * 7f + 1f);          // radial streaks in the iris
            Color iris = Color.Lerp(new Color(0.55f, 0.36f, 0.18f), new Color(0.30f, 0.19f, 0.10f), Mathf.InverseLerp(11f, 27f, deg)) * ring;
            iris = Color.Lerp(iris, new Color(0.13f, 0.09f, 0.07f), Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, 31.5f, deg)));
            Color c = iris;
            c.a = 0f;
            float pupil = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 12f, deg));
            c = Color.Lerp(c, new Color(0.02f, 0.02f, 0.02f, 0f), pupil);
            float sclera = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(31f, 34f, deg));
            var white = Color.Lerp(new Color(0.94f, 0.92f, 0.89f, 0f), new Color(0.86f, 0.70f, 0.68f, 0f), Mathf.InverseLerp(55f, 100f, deg));
            // the caruncle, the pink fleshy corner beside the nose
            float caruncle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.86f, 0.95f, Vector3.Dot(d, inner))) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(45f, 60f, deg));
            white = Color.Lerp(white, new Color(0.80f, 0.46f, 0.42f, 0f), caruncle);
            c = Color.Lerp(c, white, sclera);
            c.a = 0f;
            return c;
        }

        /// <summary>Append <paramref name="extra"/> to <paramref name="skin"/> as one mesh.</summary>
        private static Mesh Merge(Mesh skin, Mesh extra)
        {
            var merged = new Mesh { name = skin.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            merged.CombineMeshes(new[]
            {
                new CombineInstance { mesh = skin, transform = Matrix4x4.identity },
                new CombineInstance { mesh = extra, transform = Matrix4x4.identity },
            }, true, false);
            merged.RecalculateBounds();
            return merged;
        }

        // ------------------------------------------------------------------ hair and colouring

        /// <summary>1 on the scalp above the hairline, 0 elsewhere (the hairline is higher on the forehead than at the
        /// nape, and rises above the ears).</summary>
        private static float HairWeight(Vector3 p, bool female = false) => female ? LongHairWeight(p) : HairWeightShort(p);

        private static float HairWeightShort(Vector3 p)
        {
            if (Mathf.Abs(p.x) > 0.115f || p.z < -0.11f || p.z > 0.125f || p.y > 1.82f) return 0f;
            float hairline = Mathf.Lerp(1.700f, 1.585f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.075f, 0.005f, p.z)));
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(hairline, hairline + 0.012f, p.y));
            // Keep the ears clear of hair.
            float ear = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.066f, 0.078f, Mathf.Abs(p.x)))
                        * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.060f, 0.040f, Mathf.Abs(p.z - 0.016f)))
                        * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.672f, 1.655f, p.y));
            return w * (1f - ear);
        }

        /// <summary>Hair that falls behind the ears and down the back of the neck to the shoulder blades.</summary>
        private static float LongHairWeight(Vector3 p)
        {
            float above = HairWeightShort(p);
            if (Mathf.Abs(p.x) > 0.115f || p.z < -0.02f || p.z > 0.125f || p.y > 1.70f || p.y < 1.38f) return above;
            float behind = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.012f, 0.014f, p.z));
            float side = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.115f, 0.070f, Mathf.Abs(p.x)));
            float bottom = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.38f, 1.48f, p.y));
            return Mathf.Max(above, behind * side * bottom);
        }

        /// <summary>Hair as a few millimetres of extra thickness on the scalp.</summary>
        private static SdfFunc WithHair(SdfFunc body, bool female = false) => p =>
        {
            float w = HairWeight(p, female);
            return w > 0f ? body(p) - 0.0045f * w : body(p);
        };

        /// <summary>Vertex colours multiplied into the skin tone: dark hair and brows, red lips.</summary>
        private static void Paint(Mesh mesh, bool female = false)
        {
            Vector3[] v = mesh.vertices;
            var colours = new Color[v.Length];
            var hair = new Color(0.38f, 0.29f, 0.23f);
            var brow = new Color(0.30f, 0.24f, 0.20f);
            var lip = female ? new Color(0.92f, 0.50f, 0.60f) : new Color(0.88f, 0.66f, 0.70f);
            var nostril = new Color(0.45f, 0.30f, 0.28f);
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = v[i];
                Color c = Color.white;
                float h = HairWeight(p, female);
                if (h > 0f) c = Color.Lerp(c, hair, h);

                float ax = Mathf.Abs(p.x);
                float browW = Mathf.Clamp01(1f - Mathf.Abs(p.y - (female ? 1.655f : 1.651f)) / (female ? 0.0038f : 0.0075f)) * Mathf.Clamp01(1f - Mathf.Abs(ax - 0.035f) / 0.024f) * (p.z < -0.070f ? 1f : 0f);
                if (browW > 0f) c = Color.Lerp(c, brow, browW);

                float lipW = Mathf.Clamp01(1f - Mathf.Abs(p.y - 1.5595f) / 0.0145f) * Mathf.Clamp01(1f - ax / 0.030f) * (p.z < -0.092f ? 1f : 0f);
                if (lipW > 0f) c = Color.Lerp(c, lip, Mathf.SmoothStep(0f, 1f, lipW));
                // nostril openings, under the nose tip
                float nos = Mathf.Clamp01(1f - Mathf.Abs(p.y - 1.6005f) / 0.005f) * Mathf.Clamp01(1f - Mathf.Abs(ax - 0.0085f) / 0.006f) * (p.z < -0.085f ? 1f : 0f);
                if (nos > 0f) c = Color.Lerp(c, nostril, Mathf.SmoothStep(0f, 1f, nos));
                colours[i] = c;
            }
            mesh.colors = colours;
        }

        /// <summary>The atlas has no outer ear, so add one at each side of the head, placed where the skin is.</summary>
        private static SdfFunc WithEars(SdfFunc body)
        {
            float x = Surface(body, new Vector3(0.20f, 1.62f, 0.012f), Vector3.left, 0.2f);
            if (x < 0f) return body;
            float surfaceX = 0.20f - x;

            // A flange standing off the side of the head, its back edge flared out, with a shallow bowl in front.
            var pinna = Sdf.Ellipsoid(new Vector3(surfaceX + 0.003f, 1.618f, 0.016f), new Vector3(0.0085f, 0.033f, 0.021f), Quaternion.Euler(0f, 18f, -6f));
            var bowl = Sdf.Ellipsoid(new Vector3(surfaceX + 0.0125f, 1.616f, 0.010f), new Vector3(0.0065f, 0.016f, 0.010f), Quaternion.Euler(0f, 18f, 0f));
            var ear = Sdf.MirrorX(Sdf.Subtract(pinna, bowl));
            return Sdf.SmoothUnion(0.006f, body, ear);
        }

        /// <summary>The female exterior (already fitted without genitals): a fuller chest.</summary>
        private static SdfFunc WithFemaleExterior(SdfFunc male)
        {
            SdfFunc smooth = male;

            float z = Surface(male, new Vector3(0.09f, 1.26f, -0.30f), Vector3.forward, 0.4f);
            if (z < 0f) return smooth;
            float surfaceZ = -0.30f + z;

            // Vulva: the mons pubis and the two labia majora with the cleft between them (the atlas fit leaves the
            // crotch as a bare, featureless surface).
            SdfFunc vulva = smooth;
            float vz = Surface(male, new Vector3(0f, 0.815f, -0.30f), Vector3.forward, 0.4f);
            if (vz > 0f)
            {
                float zf = -0.30f + vz;
                var mons = Sdf.Ellipsoid(new Vector3(0f, 0.822f, zf + 0.002f), new Vector3(0.030f, 0.020f, 0.012f));
                var labia = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0105f, 0.790f, zf + 0.004f), new Vector3(0.0085f, 0.032f, 0.0110f), Quaternion.Euler(-8f, 0f, 0f)));
                var cleft = Sdf.Ellipsoid(new Vector3(0f, 0.790f, zf + 0.0135f), new Vector3(0.0012f, 0.020f, 0.0030f));
                vulva = Sdf.Subtract(Sdf.SmoothUnion(0.012f, smooth, mons, labia), cleft);
            }
            smooth = vulva;

            var breast = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.09f, 1.255f, surfaceZ + 0.010f), new Vector3(0.062f, 0.058f, 0.036f)));
            return Sdf.SmoothUnion(0.03f, smooth, breast);
        }

        public static void Build(Transform root, Material skin, int layer)
        {
            if (ZAnatomy.Available)
            {
                // A shell fitted around the real atlas anatomy, so it takes its shape from the muscles beneath.
                SdfFunc fitted = ZAnatomy.SkinField();
                SdfFunc uncut = WithHair(WithEars(WithFaceSex(WithFace(fitted), false)));
                SdfFunc womanUncut = WithFemaleExterior(WithHair(WithEars(WithFaceSex(WithFace(ZAnatomy.SkinField(true)), true)), true));
                // The lids are opened for the eyeballs; containment is still checked against the uncut skin.
                SdfFunc male = WithNostrilsAndMouth(WithFissures(uncut), false);
                SdfFunc womanField = WithNostrilsAndMouth(WithFissures(womanUncut), true);
                Field = uncut;

                var gridMin = new Vector3(-0.46f, -0.02f, -0.24f);
                var gridMax = new Vector3(0.46f, 1.80f, 0.24f);
                // The skin shader needs no UVs, so leave them out: the mesh is large.
                Mesh maleBuilt = SurfaceNets.Build(male, gridMin, gridMax, 0.004f, "SkinShell", 2f);
                Mesh femaleBuilt = SurfaceNets.Build(womanField, gridMin, gridMax, 0.004f, "SkinShellFemale", 2f);
                Paint(maleBuilt);
                Paint(femaleBuilt, true);
                maleBuilt.uv = null;
                femaleBuilt.uv = null;
                Mesh eyes = EyeParts(male);
                maleBuilt = Merge(maleBuilt, eyes);
                femaleBuilt = Merge(femaleBuilt, eyes);
                Object.DestroyImmediate(eyes);
                Mesh maleMesh = PartFactory.Save(maleBuilt, "SkinShell");
                Mesh femaleMesh = PartFactory.Save(femaleBuilt, "SkinShellFemale");
                PartFactory.Add(root, "Skin", "SYS_INTEG_SKIN", maleMesh, skin, layer);
                PartFactory.Add(root, SkinFemaleName, "SYS_INTEG_SKIN", femaleMesh, skin, layer);
                return;
            }

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
