using System.Collections.Generic;

namespace HumanBodyExplorer.Dicom
{
    /// <summary>
    /// Phase 81 (simplified): scrubs the standard PHI-bearing DICOM tags -
    /// (0010,0010) Patient Name, (0010,0020) Patient ID, (0010,0030) DOB - before
    /// a file is cached. Operates on a parsed tag dictionary rather than raw
    /// DICOM binary, since no DICOM parsing library (native or managed) is
    /// available in this environment to produce that dictionary from a real
    /// .dcm file - Phase 76's native parser is a stub for the same reason. The
    /// scrubbing logic itself is real and tested against the tag IDs the spec
    /// names.
    /// </summary>
    public static class DicomAnonymizer
    {
        public const string PatientNameTag = "0010,0010";
        public const string PatientIdTag = "0010,0020";
        public const string PatientDobTag = "0010,0030";

        private static readonly string[] PhiTags = { PatientNameTag, PatientIdTag, PatientDobTag };

        public static Dictionary<string, string> Anonymize(Dictionary<string, string> tags)
        {
            var result = new Dictionary<string, string>(tags);

            foreach (var tag in PhiTags)
            {
                if (result.ContainsKey(tag))
                {
                    result[tag] = string.Empty;
                }
            }

            return result;
        }
    }
}
