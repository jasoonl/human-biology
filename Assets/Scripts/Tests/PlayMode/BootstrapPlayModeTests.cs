using System.Collections;
using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HumanBodyExplorer.Tests
{
    /// <summary>
    /// Verifies the actual runtime boot sequence (Phases 1-10) end-to-end by loading
    /// Bootstrap.unity in Play Mode, not just testing the classes in isolation.
    /// </summary>
    public class BootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_LoadsAnatomyData_AndReachesFreeRoamState()
        {
            SceneManager.LoadScene("Bootstrap");
            yield return null; // let the scene load
            yield return null; // let Awake/async boot run at least one frame

            float timeout = Time.realtimeSinceStartup + 5f;
            while (GameManager.Instance == null || GameManager.Instance.CurrentGameState != GameState.FreeRoam)
            {
                if (Time.realtimeSinceStartup > timeout)
                {
                    Assert.Fail("Timed out waiting for GameManager to reach FreeRoam state.");
                }
                yield return null;
            }

            Assert.IsNotNull(GameManager.Instance);
            Assert.AreEqual(GameState.FreeRoam, GameManager.Instance.CurrentGameState);
            Assert.IsNotNull(GameManager.Instance.DataController);
            Assert.Greater(GameManager.Instance.DataController.AllNodes.Count, 0,
                "Expected the anatomy_dictionary.json StreamingAssets fixture to load at least one node.");

            var heart = GameManager.Instance.DataController.GetNode("SYS_CV_HEART_LV");
            Assert.AreEqual("Left Ventricle", heart.CommonName);
        }
    }
}
