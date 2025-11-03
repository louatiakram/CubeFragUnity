using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Scene setup script - automatically creates the exploding sphere scene
    /// Add this to an empty GameObject in your scene
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;

        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupScene();
            }
        }

        /// <summary>
        /// Setup the complete scene programmatically
        /// </summary>
        public void SetupScene()
        {
            Debug.Log("Setting up Exploding Sphere Scene...");

            // Clear existing sphere controllers
            ExplodingSphereController[] existingControllers = FindObjectsByType<ExplodingSphereController>(FindObjectsSortMode.None);
            foreach (var existingController in existingControllers)
            {
                if (existingController.gameObject != this.gameObject)
                {
                    Destroy(existingController.gameObject);
                }
            }

            // Create main controller
            GameObject controllerObject = new GameObject("ExplodingSphereController");
            controllerObject.AddComponent<ExplodingSphereController>();

            Debug.Log("Scene setup complete! Watch the sphere spin and explode!");
        }

        /// <summary>
        /// Public method to trigger setup from inspector
        /// </summary>
        [ContextMenu("Setup Scene Now")]
        public void SetupSceneFromMenu()
        {
            SetupScene();
        }
    }
}

