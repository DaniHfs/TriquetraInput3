using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ModLoader.Framework;
using ModLoader.Framework.Attributes;
using SharpDX.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.Newtonsoft.Json;
using Debug = UnityEngine.Debug;
using Encoding = System.Text.Encoding;
using Formatting = Valve.Newtonsoft.Json.Formatting;

namespace Triquetra.Input
{
    [ItemId("midnight-triquetra3")]
    public class Plugin : VtolMod
    {
        private GameObject imguiObject;
        private static string bindingsPath;
        private static string jsonBindingsPath;

        public static bool asyncLoadingBindings;

        public static void Write(object msg)
        {
            Debug.Log(msg);
        }

        public void Awake()
        {
            Enable();
        }

        public void Enable()
        {
            imguiObject = new GameObject();
            imguiObject.AddComponent<TriquetraInputBinders>();
            GameObject.DontDestroyOnLoad(imguiObject);

            string basePath = PilotSaveManager.saveDataPath;
            bindingsPath = Path.Combine(basePath, "triquetrainput.xml");
            jsonBindingsPath = Path.Combine(basePath, "triquetrainput.json");
            StartCoroutine(LoadBindingsCoroutine());
        }

        public void Disable()
        {
            Debug.Log("Destroying Triquetra Input Object");
            GameObject.Destroy(imguiObject);
        }

        public static bool IsFlyingScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex is 7 or 11;
        }

        public static void SaveBindings()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(Binding.Bindings.GetType());
                using (StringWriter debugWriter = new StringWriter())
                {
                    serializer.Serialize(debugWriter, Binding.Bindings);
                    Debug.Log(debugWriter.ToString());
                }

                using (TextWriter fileWriter = new StreamWriter(bindingsPath))
                {
                    serializer.Serialize(fileWriter, Binding.Bindings);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving bindings: {ex.Message}");
            }
        }

        public static void LoadBindings()
        {
            if (!File.Exists(bindingsPath))
                return;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Binding>));
                using (Stream reader = new FileStream(bindingsPath, FileMode.Open))
                {
                    Binding.Bindings = (List<Binding>)serializer.Deserialize(reader) ?? new List<Binding>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading bindings: {ex.Message}");
                Binding.Bindings = new List<Binding>();
            }
        }

        private static IEnumerator LoadBindingsCoroutine()
        {
            asyncLoadingBindings = true;
            var task = Task.Run(AsyncLoadBindings);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            asyncLoadingBindings = false;
        }

        private static async Task AsyncLoadBindings()
        {
            if (!File.Exists(bindingsPath))
                return;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Binding>));
                using (Stream reader = new FileStream(bindingsPath, FileMode.Open))
                {
                    var bindings = await Task.Run(() => (List<Binding>)serializer.Deserialize(reader));
                    Binding.Bindings = bindings ?? new List<Binding>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading bindings asynchronously: {ex.Message}");
                Binding.Bindings = new List<Binding>();
            }
        }

        public override void UnLoad()
        {
            Disable();
        }
    }
}