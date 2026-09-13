using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace HumanBodyExplorer.Data
{
    [System.Serializable]
    public class Vector3Data
    {
        [JsonProperty("x")] public float x;
        [JsonProperty("y")] public float y;
        [JsonProperty("z")] public float z;

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [System.Serializable]
    public class AnatomyNode
    {
        [JsonProperty("entityID")] public string EntityID;
        [JsonProperty("snomedCTCode")] public string SnomedCTCode;
        [JsonProperty("icd10PathologyCodes")] public List<string> Icd10PathologyCodes;
        [JsonProperty("latinName")] public string LatinName;
        [JsonProperty("commonName")] public string CommonName;
        [JsonProperty("systemCategory")] public List<string> SystemCategory;
        [JsonProperty("descriptionProfessional")] public string DescriptionProfessional;
        [JsonProperty("descriptionPatient")] public string DescriptionPatient;
        [JsonProperty("boundsCenterOffset")] public Vector3Data BoundsCenterOffset;
        [JsonProperty("idealCameraDistance")] public float IdealCameraDistance;
        [JsonProperty("addressableMeshKey")] public string AddressableMeshKey;
        [JsonProperty("audioTTSKey")] public string AudioTTSKey;
        [JsonProperty("connectedNodes")] public List<string> ConnectedNodes;
        [JsonProperty("hasMicroDive")] public bool HasMicroDive;
        [JsonProperty("microDiveKey")] public string MicroDiveKey;
        [JsonProperty("pharmacologyTargets")] public List<string> PharmacologyTargets;
    }

    public class AnatomyNotFoundException : System.Exception
    {
        public AnatomyNotFoundException(string entityId)
            : base($"Anatomy node '{entityId}' was not found in the loaded dictionary.")
        {
        }
    }
}
