using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Builds the skeleton from sculpted meshes rather than primitives. Split across
    /// partial files by region: limbs and girdles, spine and thorax, skull, hands and feet.
    /// </summary>
    public static partial class SkeletonBuilder
    {
        public static void Build(Transform root, Material bone, Material cartilage, int layer)
        {
            BuildGirdlesAndLimbs(root, bone, layer);
            BuildVertebralColumn(root, bone, cartilage, layer);
            BuildThorax(root, bone, cartilage, layer);
            BuildSkull(root, bone, cartilage, layer);
            BuildHand(root, bone, layer);
            BuildFoot(root, bone, layer);
        }
    }
}
