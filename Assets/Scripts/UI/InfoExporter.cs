using System.Collections;
using System.IO;
using HumanBodyExplorer.Data;
using UnityEngine;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 56 (deviates from spec): the spec calls for iTextSharp to generate a
    /// formatted PDF. iTextSharp is a NuGet package with no NuGet-for-Unity
    /// installed in this environment, and no license/version has been vetted for
    /// this project, so this instead writes a plain-text fact sheet plus a PNG
    /// screenshot of the current view to the platform's Documents folder - the
    /// same underlying data (description, pathologies, current view), just not
    /// PDF-formatted.
    /// </summary>
    public class InfoExporter : MonoBehaviour
    {
        public IEnumerator ExportFactSheet(AnatomyNode node, UnityEngine.Camera viewCamera)
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string exportDir = Path.Combine(documentsPath, "HumanBodyExplorer_Exports");
            Directory.CreateDirectory(exportDir);

            string safeName = node.CommonName.Replace(" ", "_");
            string textPath = Path.Combine(exportDir, $"{safeName}_FactSheet.txt");
            string imagePath = Path.Combine(exportDir, $"{safeName}_View.png");

            File.WriteAllText(textPath, BuildFactSheetText(node));

            if (viewCamera != null)
            {
                yield return CaptureScreenshot(viewCamera, imagePath);
            }

            Debug.Log($"[InfoExporter] Exported fact sheet to {textPath}");
        }

        public static string BuildFactSheetText(AnatomyNode node)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# {node.CommonName} ({node.LatinName})");
            sb.AppendLine();
            sb.AppendLine(node.DescriptionPatient);
            sb.AppendLine();

            if (node.Icd10PathologyCodes != null && node.Icd10PathologyCodes.Count > 0)
            {
                sb.AppendLine("Related pathologies (ICD-10):");
                foreach (var code in node.Icd10PathologyCodes) sb.AppendLine($"  - {code}");
            }

            return sb.ToString();
        }

        private IEnumerator CaptureScreenshot(UnityEngine.Camera cam, string path)
        {
            yield return new WaitForEndOfFrame();

            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}
