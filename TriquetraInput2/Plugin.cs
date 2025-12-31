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
    [ItemId("danku-triquetra2")]
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

            bindingsPath = PilotSaveManager.saveDataPath + "/triquetrainput.xml";
            jsonBindingsPath = PilotSaveManager.saveDataPath + "/triquetrainput.json";
            //LoadBindings();
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
            return buildIndex == 7 || buildIndex == 11;
        }

        public static void SaveBindings()
        {
            /*if (Binding.Bindings != null)
            {
                var json = JsonConvert.SerializeObject(Binding.Bindings, Formatting.Indented);
                File.WriteAllText(jsonBindingsPath, json);
            }*/
            XmlSerializer serializer = new XmlSerializer(Binding.Bindings.GetType());
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, Binding.Bindings);
                Debug.Log(writer.ToString());
            }
            using (TextWriter writer = new StreamWriter(bindingsPath))
            {
                serializer.Serialize(writer, Binding.Bindings);
            }
        }

        public static void LoadBindings()
        {
            Stopwatch sw = Stopwatch.StartNew();
            XmlSerializer serializer = new XmlSerializer(typeof(List<Binding>));
            if (File.Exists(bindingsPath))
            {
                using (Stream reader = new FileStream(bindingsPath, FileMode.Open))
                {
                    Binding.Bindings = (List<Binding>)serializer.Deserialize(reader);
                }
            }
            
            /*if (File.Exists(jsonBindingsPath))
            {
                var text = File.ReadAllText(jsonBindingsPath);
                if (String.IsNullOrEmpty(text))
                    return;
                Binding.Bindings = JsonConvert.DeserializeObject<List<Binding>>(text);
            }*/
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

        private static async Task AsyncLoadBindings() // I have no clue if this is even any faster than normal
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Binding>));
            if (File.Exists(bindingsPath))
            {
                using (Stream reader = new FileStream(bindingsPath, FileMode.Open))
                {
                    var bindings = await Task.Run(() => (List<Binding>)serializer.Deserialize(reader));
                    Binding.Bindings = bindings;
                }
            }
        }

        public override void UnLoad()
        {
            Disable();
        }
    }
}