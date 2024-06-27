using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Models;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine.SceneManagement;
using UnityEngine;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Newtonsoft.Json;
using System.Globalization;
using Archipelago.MultiClient.Net.Packets;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using Archipelago.MultiClient.Net.Helpers;
using static TunicRandomizer.SaveFlags;
using TunicRandomizer;

namespace JayoVNyan
{
    class VNyanIntegration
    {
        private static ManualLogSource Logger = TunicRandomizer.TunicRandomizer.Logger;
        private static VNyanIntegration instance;

        public static bool initialized = false;
        public static bool connected;
        
        public VNyanIntegration GetInstance()
        {
            if(instance == null)
            {
                instance = new VNyanIntegration();
            }
            return instance;
        }

        public static void TryConnect()
        {
            if(!initialized)
            {
                VNyanSender.LogInfo += (msg) => { Logger.LogInfo(msg); };
                VNyanSender.OnWebsocketOpen += () => { Logger.LogInfo($"VNyan Socket Connected"); connected = true; };
                VNyanSender.OnWebsocketClose += (reason) => { Logger.LogInfo($"VNyan Socket Disconnected: {reason}"); connected = false; };
                VNyanSender.OnWebsocketError += (message) => { Logger.LogInfo($"VNyan Socket Error: {message}"); };
                initialized = true;
            }
            
            TryDisconnect();

            RandomizerSettings settings = JsonConvert.DeserializeObject<RandomizerSettings>(File.ReadAllText(TunicRandomizer.TunicRandomizer.SettingsPath));
            TunicRandomizer.TunicRandomizer.Settings.VNyanSettings = settings.VNyanSettings;
            
            VNyanSender.connectSocket(settings.VNyanSettings.Address);
        }

        public static void TryDisconnect()
        {
            VNyanSender.DisconnectSocket();
        }

    }
}
