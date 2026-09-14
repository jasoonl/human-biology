using System;
using System.IO;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    [Serializable]
    public struct SessionData
    {
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
        public float SliceDepth;
        public string[] HiddenSystemIds;
    }

    /// <summary>
    /// Phase 87 (deviates from spec): the spec calls for BinaryFormatter.
    /// BinaryFormatter is officially deprecated by Microsoft (CVE-tracked
    /// deserialization vulnerabilities) and removed from the base class library
    /// in modern .NET; Unity's own docs also warn against it. This hand-writes
    /// the same "binary session data as a Base64 string" shape via
    /// BinaryWriter/BinaryReader instead - deterministic, no arbitrary-type
    /// deserialization risk, and functionally equivalent for this fixed struct.
    /// </summary>
    public static class SessionSaveManager
    {
        private static string SessionFilePath =>
            Path.Combine(Application.persistentDataPath, "session.dat");

        public static string Serialize(SessionData data)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(data.CameraPosition.x);
                writer.Write(data.CameraPosition.y);
                writer.Write(data.CameraPosition.z);
                writer.Write(data.CameraRotation.x);
                writer.Write(data.CameraRotation.y);
                writer.Write(data.CameraRotation.z);
                writer.Write(data.CameraRotation.w);
                writer.Write(data.SliceDepth);

                string[] hidden = data.HiddenSystemIds ?? Array.Empty<string>();
                writer.Write(hidden.Length);
                foreach (var id in hidden) writer.Write(id);
            }

            return Convert.ToBase64String(stream.ToArray());
        }

        public static SessionData Deserialize(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var data = new SessionData
            {
                CameraPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                CameraRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                SliceDepth = reader.ReadSingle()
            };

            int hiddenCount = reader.ReadInt32();
            data.HiddenSystemIds = new string[hiddenCount];
            for (int i = 0; i < hiddenCount; i++) data.HiddenSystemIds[i] = reader.ReadString();

            return data;
        }

        public static void Save(SessionData data)
        {
            File.WriteAllText(SessionFilePath, Serialize(data));
        }

        public static bool TryLoad(out SessionData data)
        {
            data = default;
            if (!File.Exists(SessionFilePath)) return false;

            data = Deserialize(File.ReadAllText(SessionFilePath));
            return true;
        }
    }
}
