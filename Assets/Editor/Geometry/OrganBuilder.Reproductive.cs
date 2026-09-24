using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The male and female reproductive systems. Each is a separate set of parts (ids SYS_REP_M_* and
    /// SYS_REP_F_*), and the Reproductive layer shows one at a time - they occupy the same space in the
    /// pelvis, so they cannot both be drawn. The figure's exterior is male (see <see cref="SkinBuilder"/>),
    /// so the male organs sit in the scrotum and penis it encloses; the female organs are internal.
    /// Left-hand structures are modelled at positive X and mirrored.
    /// </summary>
    public static partial class OrganBuilder
    {
        private static void BuildReproductive()
        {
            BuildMaleReproductive();
            BuildFemaleReproductive();
        }

        // ---------------------------------------------------------------- male

        private static void BuildMaleReproductive()
        {
            var testisMat = _tissue(new Color(0.88f, 0.80f, 0.64f), 0.45f);
            var duct = _tissue(new Color(0.92f, 0.88f, 0.74f), 0.45f);
            var gland = _tissue(new Color(0.82f, 0.66f, 0.54f), 0.40f);
            var erectile = _tissue(new Color(0.84f, 0.46f, 0.44f), 0.42f);

            // Testes: the male gonads, hung outside the body in the scrotum where it is about 2 degrees cooler than the
            // core - sperm development needs that. Each contains coiled seminiferous tubules, where sperm are made, and
            // interstitial cells that make testosterone.
            SdfFunc testis = Sdf.Ellipsoid(V(0.021f, 0.748f, -0.038f), V(0.013f, 0.021f, 0.014f), Quaternion.Euler(0f, 0f, -10f));
            PartFactory.AddPair(_root, "Testis", "SYS_REP_M_TESTIS",
                Sculpt("Testis", testis, 0.0007f, V(0.021f, 0.748f, -0.038f)), testisMat, _layer);

            // Epididymis: the tightly coiled tube on the back of each testis where sperm mature and are stored.
            Mesh epididymis = LoftMesh("Epididymis", t => Mathf.Lerp(0.0038f, 0.0026f, t), 10, 1.5f, Vector3.right,
                V(0.024f, 0.768f, -0.030f), V(0.030f, 0.752f, -0.026f), V(0.030f, 0.736f, -0.028f), V(0.026f, 0.726f, -0.034f));
            PartFactory.AddPair(_root, "Epididymis", "SYS_REP_M_EPIDIDYMIS", PartFactory.Save(epididymis, "Epididymis"), gland, _layer);

            // Vas deferens: the muscular tube that carries sperm from the epididymis up through the inguinal canal, over the
            // ureter and behind the bladder to the prostate - cut in a vasectomy.
            Mesh vas = LoftMesh("VasDeferens", Loft.Constant(0.0019f), 8, 1f, null,
                V(0.026f, 0.728f, -0.034f), V(0.031f, 0.752f, -0.026f), V(0.032f, 0.790f, -0.030f), V(0.040f, 0.830f, -0.040f),
                V(0.052f, 0.868f, -0.044f), V(0.056f, 0.886f, -0.034f), V(0.052f, 0.894f, -0.010f), V(0.044f, 0.886f, 0.008f),
                V(0.030f, 0.868f, 0.006f), V(0.016f, 0.852f, -0.004f), V(0.008f, 0.844f, -0.024f));
            PartFactory.AddPair(_root, "VasDeferens", "SYS_REP_M_VAS_DEFERENS", PartFactory.Save(vas, "VasDeferens"), duct, _layer);

            // Seminal vesicles: two lobulated glands behind the bladder that add about 60 per cent of the volume of semen -
            // a fructose-rich fluid that feeds the sperm.
            SdfFunc vesicle = Sdf.Displace(Sdf.Chain(new[] { V(0.026f, 0.880f, 0.004f), V(0.018f, 0.864f, -0.002f), V(0.010f, 0.850f, -0.014f) },
                new[] { 0.0072f, 0.0062f, 0.0048f }, 0.003f), 0.0012f, 320f, 2);
            PartFactory.AddPair(_root, "SeminalVesicle", "SYS_REP_M_SEMINAL_VESICLE",
                Sculpt("SeminalVesicle", vesicle, 0.0006f, V(0.018f, 0.865f, -0.002f)), gland, _layer);

            // Prostate: a walnut-sized gland encircling the urethra just below the bladder that adds an alkaline fluid to semen.
            SdfFunc prostate = Sdf.Subtract(Sdf.Ellipsoid(V(0f, 0.838f, -0.032f), V(0.022f, 0.016f, 0.016f)),
                Sdf.Capsule(V(0f, 0.860f, -0.036f), V(0f, 0.820f, -0.042f), 0.0032f));
            Place("Prostate", "SYS_REP_M_PROSTATE", Sculpt("Prostate", prostate, 0.0006f, V(-0.02f, 0.836f, -0.032f), V(0.02f, 0.840f, -0.032f)), gland);

            // Bulbourethral (Cowper's) glands: pea-sized glands whose clear fluid neutralises acidic urine in the urethra.
            PartFactory.AddPair(_root, "BulbourethralGland", "SYS_REP_M_BULBOURETHRAL_GLAND",
                Sculpt("BulbourethralGland", Sdf.Sphere(V(0.009f, 0.808f, -0.042f), 0.0045f), 0.0004f, V(0.009f, 0.808f, -0.042f)), gland, _layer);

            // Urethra: the tube from the bladder neck through the prostate and the length of the penis, shared by urine and semen.
            Mesh urethra = LoftMesh("MaleUrethra", Loft.Constant(0.0022f), 8, 1f, null,
                V(0f, 0.856f, -0.036f), V(0f, 0.836f, -0.034f), V(0f, 0.814f, -0.044f), V(0f, 0.796f, -0.054f),
                V(0f, 0.775f, -0.072f), V(0f, 0.752f, -0.086f), V(0f, 0.736f, -0.096f));
            Place("MaleUrethra", "SYS_REP_M_URETHRA", PartFactory.Save(urethra, "MaleUrethra"), _tissue(new Color(0.95f, 0.88f, 0.56f), 0.5f));

            // Penis: two corpora cavernosa on top, which fill with blood in erection, and the corpus spongiosum below,
            // enclosing the urethra and swelling into the glans.
            Mesh cavernosum = LoftMesh("CorpusCavernosum", t => Mathf.Lerp(0.0070f, 0.0078f, Mathf.Sin(t * Mathf.PI)), 12, 1f, null,
                V(0.018f, 0.794f, -0.030f), V(0.008f, 0.790f, -0.054f), V(0.006f, 0.772f, -0.076f), V(0.006f, 0.752f, -0.090f), V(0.006f, 0.742f, -0.094f));
            PartFactory.AddPair(_root, "CorpusCavernosum", "SYS_REP_M_PENIS_CAVERNOSA", PartFactory.Save(cavernosum, "CorpusCavernosum"), erectile, _layer);

            SdfFunc spongiosum = Sdf.SmoothUnion(0.004f,
                Sdf.Sphere(V(0f, 0.802f, -0.044f), 0.0088f),
                Sdf.Chain(new[] { V(0f, 0.796f, -0.054f), V(0f, 0.775f, -0.074f), V(0f, 0.752f, -0.088f), V(0f, 0.742f, -0.094f) },
                    new[] { 0.0062f, 0.0058f, 0.0056f, 0.0070f }, 0.003f),
                Sdf.RoundCone(V(0f, 0.742f, -0.094f), 0.0090f, V(0f, 0.731f, -0.099f), 0.0078f));
            Place("CorpusSpongiosum", "SYS_REP_M_PENIS_SPONGIOSUM",
                Sculpt("CorpusSpongiosum", spongiosum, 0.0005f, V(0f, 0.800f, -0.044f), V(0f, 0.728f, -0.100f)), _tissue(new Color(0.88f, 0.56f, 0.54f), 0.42f));
        }

        // ---------------------------------------------------------------- female

        private static void BuildFemaleReproductive()
        {
            var uterusMat = _tissue(new Color(0.82f, 0.44f, 0.44f), 0.40f);
            var lining = _tissue(new Color(0.78f, 0.20f, 0.28f), 0.45f);
            var tissue = _tissue(new Color(0.84f, 0.52f, 0.50f), 0.42f);
            var tubeMat = _tissue(new Color(0.92f, 0.66f, 0.60f), 0.42f);
            var ovaryMat = _tissue(new Color(0.92f, 0.80f, 0.68f), 0.40f);

            // Uterus: the pear-shaped muscular organ, tipped forward over the bladder, where a fertilised egg implants and the
            // fetus grows. Its thick wall (myometrium) is smooth muscle that contracts in labour.
            SdfFunc uterus = Sdf.SmoothUnion(0.010f,
                Sdf.Ellipsoid(V(0f, 0.928f, -0.004f), V(0.026f, 0.022f, 0.015f), Quaternion.Euler(-14f, 0f, 0f)),
                Sdf.Ellipsoid(V(0f, 0.908f, 0.002f), V(0.018f, 0.024f, 0.014f), Quaternion.Euler(-14f, 0f, 0f)));
            Place("Uterus", "SYS_REP_F_UTERUS", Sculpt("Uterus", uterus, 0.0008f, V(-0.02f, 0.962f, -0.02f), V(0.02f, 0.890f, 0.02f)), uterusMat);

            // Endometrium: the lining of the uterine cavity, thickened each cycle under the influence of oestrogen and progesterone
            // and shed as menstruation if there is no pregnancy.
            SdfFunc cavity = Sdf.SmoothUnion(0.004f,
                Sdf.Ellipsoid(V(0f, 0.920f, -0.002f), V(0.016f, 0.018f, 0.0042f), Quaternion.Euler(-14f, 0f, 0f)),
                Sdf.Capsule(V(0f, 0.908f, 0.002f), V(0f, 0.890f, 0.010f), 0.0022f));
            Place("Endometrium", "SYS_REP_F_ENDOMETRIUM", Sculpt("Endometrium", cavity, 0.0005f, V(0f, 0.925f, 0f), V(0f, 0.888f, 0.010f)), lining);

            // Cervix: the narrow lower end of the uterus that projects into the vagina, its canal plugged with mucus except at
            // ovulation and opening about 10 cm in labour.
            SdfFunc cervix = Sdf.Subtract(Sdf.Ellipsoid(V(0f, 0.887f, 0.010f), V(0.011f, 0.015f, 0.011f)),
                Sdf.Capsule(V(0f, 0.900f, 0.008f), V(0f, 0.872f, 0.014f), 0.0020f));
            Place("Cervix", "SYS_REP_F_CERVIX", Sculpt("Cervix", cervix, 0.0005f, V(0f, 0.887f, 0.010f)), tissue);

            // Vagina: the muscular, elastic canal from the cervix to the outside, the birth canal.
            Mesh vagina = Strap("Vagina", 0.0050f, 2.6f, Vector3.forward,
                V(0f, 0.880f, 0.012f), V(0f, 0.852f, 0.010f), V(0f, 0.826f, 0.000f), V(0f, 0.806f, -0.014f), V(0f, 0.796f, -0.024f));
            Place("Vagina", "SYS_REP_F_VAGINA", PartFactory.Save(vagina, "Vagina"), tissue);

            // Fallopian (uterine) tubes: each runs from the top corner of the uterus to an ovary, ending in fringed fimbriae that
            // sweep the released egg in; fertilisation normally happens in the widened ampulla.
            var tube = new List<Mesh>
            {
                LoftMesh("TubeBody", t => Mathf.Lerp(0.0020f, 0.0040f, t * t), 10, 1f, null,
                    V(0.018f, 0.938f, -0.004f), V(0.036f, 0.950f, 0.000f), V(0.054f, 0.945f, 0.010f), V(0.066f, 0.930f, 0.022f), V(0.070f, 0.918f, 0.030f)),
            };
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 tip = V(0.070f, 0.918f, 0.030f) + new Vector3(Mathf.Cos(a) * 0.009f, Mathf.Sin(a) * 0.007f - 0.004f, 0.006f);
                tube.Add(LoftMesh("Fimbria" + i, Loft.Taper(0.0011f, 0.0004f), 6, 1f, null, V(0.070f, 0.918f, 0.030f), Vector3.Lerp(V(0.070f, 0.918f, 0.030f), tip, 0.5f), tip));
            }
            PartFactory.AddPair(_root, "FallopianTube", "SYS_REP_F_FALLOPIAN_TUBE", Combine("FallopianTube", tube), tubeMat, _layer);

            // Ovaries: almond-sized glands that store the egg cells, release one about every 28 days and make oestrogen and progesterone.
            SdfFunc ovary = Sdf.Displace(Sdf.Ellipsoid(V(0.066f, 0.904f, 0.034f), V(0.016f, 0.011f, 0.009f), Quaternion.Euler(0f, 0f, 24f)), 0.0012f, 340f, 5);
            PartFactory.AddPair(_root, "Ovary", "SYS_REP_F_OVARY", Sculpt("Ovary", ovary, 0.0006f, V(0.066f, 0.904f, 0.034f)), ovaryMat, _layer);
        }
    }
}
