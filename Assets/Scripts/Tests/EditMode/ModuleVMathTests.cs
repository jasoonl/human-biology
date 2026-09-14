using HumanBodyExplorer.Core;
using HumanBodyExplorer.UI;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class ModuleVMathTests
    {
        [Test]
        public void AssemblyMode_IsWithinSnapThreshold_TrueWhenCloseEnough()
        {
            bool result = AssemblyModeController.IsWithinSnapThreshold(
                new Vector3(0.05f, 0, 0), Quaternion.Euler(0, 5, 0),
                Vector3.zero, Quaternion.identity,
                positionThreshold: 0.1f, angleThreshold: 15f);

            Assert.IsTrue(result);
        }

        [Test]
        public void AssemblyMode_IsWithinSnapThreshold_FalseWhenTooFar()
        {
            bool result = AssemblyModeController.IsWithinSnapThreshold(
                new Vector3(1f, 0, 0), Quaternion.identity,
                Vector3.zero, Quaternion.identity,
                positionThreshold: 0.1f, angleThreshold: 15f);

            Assert.IsFalse(result);
        }

        [Test]
        public void AudioEnvironment_ComputeCutoff_ReducesPerLayer()
        {
            float cutoff0 = AudioEnvironmentController.ComputeCutoff(0, 22000f, 1500f, 500f);
            float cutoff3 = AudioEnvironmentController.ComputeCutoff(3, 22000f, 1500f, 500f);

            Assert.AreEqual(22000f, cutoff0);
            Assert.AreEqual(17500f, cutoff3);
            Assert.Less(cutoff3, cutoff0);
        }

        [Test]
        public void AudioEnvironment_ComputeCutoff_ClampsToMinimum()
        {
            float cutoff = AudioEnvironmentController.ComputeCutoff(100, 22000f, 1500f, 500f);
            Assert.AreEqual(500f, cutoff);
        }

        [Test]
        public void CardioAnimation_ComputeLubDubScale_NeverNegative()
        {
            for (float t = 0f; t < 2f; t += 0.05f)
            {
                float scale = CardioAnimationController.ComputeLubDubScale(t, 70f);
                Assert.GreaterOrEqual(scale, 0f);
            }
        }

        [Test]
        public void Localization_MissingKey_ReturnsVisibleMarker()
        {
            var loc = new LocalizationManager();
            loc.AddTable("en", new System.Collections.Generic.Dictionary<string, string> { { "hello", "Hello" } });

            Assert.AreEqual("Hello", loc.Get("hello"));
            Assert.AreEqual("[[missing_key]]", loc.Get("missing_key"));
        }

        [Test]
        public void Localization_ChangeLanguage_UnknownLocale_LogsWarningAndKeepsCurrent()
        {
            var loc = new LocalizationManager();
            loc.AddTable("en", new System.Collections.Generic.Dictionary<string, string>());

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*"));
            loc.ChangeLanguage("xx");

            Assert.AreEqual("en", loc.CurrentLocale);
        }
    }
}
