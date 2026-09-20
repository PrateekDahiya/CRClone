using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CRClone.Tests.PlayMode
{
    /// <summary>
    /// Guards the "blank screen" regression: the project previously shipped with no
    /// scenes at all, and UIManager parented its screens to a non-Canvas transform,
    /// so nothing was ever drawn. These tests assert something is actually on screen
    /// after Boot runs, not merely that the assets exist.
    /// </summary>
    public class BootSmokeTests
    {
        [UnityTest]
        public IEnumerator Boot_Scene_Reaches_MainMenu_With_Visible_UI()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;

            // Boot -> LoadSceneAsync(MainMenu) -> screen instantiation all span frames.
            for (int i = 0; i < 240; i++)
            {
                if (VisibleGraphics().Any()) break;
                yield return null;
            }

            var graphics = VisibleGraphics().ToList();
            Assert.IsNotEmpty(graphics,
                "No active UI Graphic is being rendered under a Canvas after Boot - the screen would be blank.");

            Assert.IsTrue(graphics.Any(g => g is Text t && !string.IsNullOrWhiteSpace(t.text)),
                "No non-empty Text is visible after Boot.");
        }

        [UnityTest]
        public IEnumerator Boot_Creates_Camera_That_Can_Render()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;

            for (int i = 0; i < 240; i++)
            {
                if (Camera.allCameras.Any(c => c.isActiveAndEnabled)) break;
                yield return null;
            }

            Assert.IsTrue(Camera.allCameras.Any(c => c.isActiveAndEnabled),
                "No enabled camera exists, so nothing can be drawn.");
        }

        [UnityTest]
        public IEnumerator Every_Build_Scene_Loads_Without_Error()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                SceneManager.LoadScene(i, LoadSceneMode.Single);
                yield return null;
                yield return null;

                Assert.IsTrue(SceneManager.GetActiveScene().isLoaded,
                    $"Scene at build index {i} ({path}) failed to load.");
            }
        }

        static System.Collections.Generic.IEnumerable<Graphic> VisibleGraphics()
        {
            return Object.FindObjectsOfType<Graphic>(false)
                .Where(g => g.isActiveAndEnabled
                            && g.canvas != null
                            && g.canvas.isActiveAndEnabled
                            && g.color.a > 0.01f);
        }
    }
}
