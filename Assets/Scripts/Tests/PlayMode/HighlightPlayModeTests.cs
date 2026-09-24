using System.Collections;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using HumanBodyExplorer.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HumanBodyExplorer.Tests
{
    /// <summary>
    /// Selecting a structure in the real scene isolates it: it takes the accent colour and
    /// every other opaque structure is dimmed; clicking empty space puts everything back.
    /// </summary>
    public class HighlightPlayModeTests
    {
        private static Color BlockColor(Renderer renderer, int materialIndex, out bool hasBlock)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, materialIndex);
            hasBlock = !block.isEmpty;
            return hasBlock ? block.GetColor("_BaseColor") : Color.clear;
        }

        /// <summary>The boot sequence builds a GameManager that survives scene loads, so a second load
        /// in the same Play session collides with the first. Clear it before and after.</summary>
        private static IEnumerator DiscardGameManager()
        {
            if (GameManager.Instance != null) Object.Destroy(GameManager.Instance.gameObject);
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator SetUp() => DiscardGameManager();

        [UnityTearDown]
        public IEnumerator TearDown() => DiscardGameManager();

        [UnityTest]
        public IEnumerator SelectingAPart_TintsItAndDimsTheRest_AndEmptySpaceRestoresThem()
        {
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            yield return null;

            float timeout = Time.realtimeSinceStartup + 5f;
            while (GameManager.Instance == null || GameManager.Instance.CurrentGameState != GameState.FreeRoam)
            {
                if (Time.realtimeSinceStartup > timeout) Assert.Fail("Timed out waiting for the scene to boot.");
                yield return null;
            }

            var highlighter = Object.FindAnyObjectByType<AnatomyHighlighter>();
            Assert.IsNotNull(highlighter, "The build should place an AnatomyHighlighter in the scene.");
            var raycaster = Object.FindAnyObjectByType<AnatomyRaycaster>();
            Assert.IsNotNull(raycaster);
            yield return null;   // the highlighter caches its renderers one frame after start
            yield return null;

            // Straight down onto the crown of the head: the first opaque structure is a skull bone.
            string selected = null;
            System.Action<string> listener = id => selected = id;
            AnatomyRaycaster.OnNodeSelected += listener;
            bool hit = raycaster.TryRaycast(new Ray(new Vector3(0f, 3f, 0.0f), Vector3.down));
            AnatomyRaycaster.OnNodeSelected -= listener;
            Assert.IsTrue(hit, "A ray down the axis of the body should meet something.");
            Assert.IsNotNull(selected);
            Assert.AreEqual(selected, highlighter.SelectedId);

            var chosen = AnatomyNodeReference.GetByEntityId(selected)[0].GetComponent<Renderer>();
            Color chosenBase = chosen.sharedMaterial.GetColor("_BaseColor");
            Color chosenNow = BlockColor(chosen, 0, out bool chosenHasBlock);
            Assert.IsTrue(chosenHasBlock, "The selected part should carry a highlight.");
            Assert.Greater(chosenNow.b - chosenNow.r, chosenBase.b - chosenBase.r,
                "Selected part should shift toward the cool accent colour.");

            // Some other opaque structure elsewhere is dimmed.
            var other = AnatomyNodeReference.GetByEntityId("SYS_SK_FEMUR")[0].GetComponent<Renderer>();
            Color otherNow = BlockColor(other, 0, out bool otherHasBlock);
            Assert.IsTrue(otherHasBlock, "Unselected parts should be ghosted.");
            Assert.AreEqual("HighlightGhost", other.sharedMaterial.name, "Unselected parts should swap to the ghost material.");
            Assert.IsFalse(other.GetComponent<Collider>().enabled, "A ghost must not intercept clicks.");
            Assert.IsTrue(chosen.GetComponent<Collider>().enabled, "The selected part must stay clickable.");
            Color otherBase = other.sharedMaterial.GetColor("_BaseColor");
            Assert.Less(otherNow.r, otherBase.r, "A ghosted part should be pulled away from its own colour.");

            // A ray that misses everything clears the selection and restores every part.
            raycaster.TryRaycast(new Ray(new Vector3(5f, 5f, 5f), Vector3.up));
            yield return null;
            Assert.IsNull(highlighter.SelectedId);
            BlockColor(other, 0, out otherHasBlock);
            Assert.IsFalse(otherHasBlock, "Clearing the selection should remove the ghosting.");
            Assert.AreNotEqual("HighlightGhost", other.sharedMaterial.name);
            Assert.IsTrue(other.GetComponent<Collider>().enabled, "Clearing should make every part clickable again.");
        }
    }
}
