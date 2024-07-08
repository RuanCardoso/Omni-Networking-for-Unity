#pragma warning disable

using Omni.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public class NetworkIsolate : MonoBehaviour
    {
        private Scene m_Scene;

        [SerializeField, HideInInspector]
        private bool m_IsOwner;

        [SerializeField]
        private string m_SceneName = "Isolate";

        [SerializeField]
        private bool m_Instantiate = false;

        [SerializeField]
        private bool m_DestroyAfterInstantiate = true;

        [SerializeField]
        private bool m_AutoSimulate = true;

        [SerializeField]
        private float m_SimulateStep = 0f;

        [SerializeField]
        private Color m_SimulateColor = Color.black;

        [SerializeField]
        private LocalPhysicsMode m_LocalPhysicsMode = LocalPhysicsMode.Physics3D;

        public Scene Scene
        {
            get => m_Scene;
        }

        public bool IsOwner
        {
            get => m_IsOwner;
        }

#if UNITY_EDITOR || !UNITY_SERVER
        private void Start()
        {
            if (!NetworkManager.IsServerActive)
                return;

            if (m_SimulateStep == 0)
            {
                m_SimulateStep = Time.fixedDeltaTime;
            }

            m_Scene = SceneManager.GetSceneByName(m_SceneName);
            if (!m_Scene.IsValid())
            {
                m_Scene = SceneManager.CreateScene(
                    m_SceneName,
                    new CreateSceneParameters(m_LocalPhysicsMode)
                );

                m_IsOwner = true;
                Move();
            }
            else
            {
                Move();
            }
        }

        private void Move()
        {
            if (m_Instantiate)
            {
                m_Instantiate = false; // false to prevent infinite loop because Awake is called after instantiate.
                // after instantiation awake must be called.
                Instantiate(gameObject, transform.position, transform.rotation);

                if (m_DestroyAfterInstantiate)
                {
                    // destroy the original game object.
                    Destroy(gameObject);
                }
            }
            else
            {
                // move the game object to the isolate scene.
                // if is instantiated, m_Instantiate must be false and this is called!
                SceneManager.MoveGameObjectToScene(gameObject, m_Scene);

                // Color for diferentiate game objects in the scenes.
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    renderer.material.color = m_SimulateColor;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!NetworkManager.IsServerActive)
                return;

            if (m_IsOwner && m_AutoSimulate && m_Scene.IsValid())
            {
                if (m_LocalPhysicsMode == LocalPhysicsMode.Physics3D)
                {
                    PhysicsScene physicsScene = m_Scene.GetPhysicsScene();
                    physicsScene.Simulate(m_SimulateStep);
                }
                else if (m_LocalPhysicsMode == LocalPhysicsMode.Physics2D)
                {
                    PhysicsScene2D physicsScene = m_Scene.GetPhysicsScene2D();
                    physicsScene.Simulate(m_SimulateStep);
                }
            }
        }
#else
        private void Start()
        {
            NetworkLogger.__Log__(
                "Isolate mode is not supported on server build. all isolate components will be ignored.",
                NetworkLogger.LogType.Warning
            );
        }
#endif
    }
}
