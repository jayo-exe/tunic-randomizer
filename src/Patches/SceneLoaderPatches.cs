﻿using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace TunicRandomizer {
    public class SceneLoaderPatches {
        public static int Fur, Puff, Details, Tunic, Scarf;
        public static int SceneId;
        public static string SceneName;
        
        public static bool SceneLoader_OnSceneLoaded_PrefixPatch(Scene loadingScene, LoadSceneMode mode, SceneLoader __instance) {
            if (SceneName == "Forest Belltower") {
                SaveFile.SetInt("chest open 19", RandomItemPatches.ItemsPickedUp["19 [Forest Belltower]"] ? 1 : 0);
            }
            if (SceneName == "Sword Cave") {
                SaveFile.SetInt("chest open 19", RandomItemPatches.ItemsPickedUp["19 [Sword Cave]"] ? 1 : 0);
            }
            return true;
        }

        public static void SceneLoader_OnSceneLoaded_PostfixPatch(Scene loadingScene, LoadSceneMode mode, SceneLoader __instance) {
            TunicRandomizer.Logger.LogInfo("Entering scene " + loadingScene.name + " (" + loadingScene.buildIndex + ")");
            SceneId = loadingScene.buildIndex;
            SceneName = loadingScene.name;
            System.Random rnd = new System.Random();
            // Fur, Puff, Details, Tunic, Scarf
            if (TunicRandomizer.Settings.RandomFoxColorsEnabled) {
                Fur = PlayerPalette.ChangeColourByDelta(0, rnd.Next(1, 16));
                Puff = PlayerPalette.ChangeColourByDelta(1, rnd.Next(1, 12));
                Details = PlayerPalette.ChangeColourByDelta(2, rnd.Next(1, 12));
                Tunic = PlayerPalette.ChangeColourByDelta(3, rnd.Next(1, 16));
                Scarf = PlayerPalette.ChangeColourByDelta(4, rnd.Next(1, 11));
            }

            if (SceneName == "Waterfall") {
                List<string> RandomObtainedFairies = new List<string>();
                foreach (string Key in RandomItemPatches.FairyLookup.Keys) {
                    StateVariable.GetStateVariableByName(RandomItemPatches.FairyLookup[Key].Flag).BoolValue = SaveFile.GetInt("randomizer obtained fairy " + Key) == 1;
                    if (SaveFile.GetInt("randomizer obtained fairy " + Key) == 1) {
                        RandomObtainedFairies.Add(Key);
                    }
                }

                StateVariable.GetStateVariableByName("SV_Fairy_5_Waterfall_Opened").BoolValue = SaveFile.GetInt("randomizer opened fairy chest Waterfall-(-47.0, 45.0, 10.0)") == 1;

                StateVariable.GetStateVariableByName("SV_Fairy_00_Enough Fairies Found").BoolValue = true;

                StateVariable.GetStateVariableByName("SV_Fairy_00_All Fairies Found").BoolValue = true;

            } else if (SceneName == "Spirit Arena") {
                for (int i = 0; i < 28; i++) {
                    SaveFile.SetInt("unlocked page " + i, SaveFile.GetInt("randomizer obtained page " + i) == 1 ? 1 : 0);
                }
                PlayerCharacterPatches.ShownHeirAssistModePrompt = false;
                PlayerCharacterPatches.HeirAssistMode = false;
                PlayerCharacterPatches.HeirAssistModeDamageValue = 0;
            } else if (SceneName == "Forest Belltower") {
                SaveFile.SetInt("chest open 19", 0);
            } else if (SceneName == "Overworld Interiors") {
                foreach (string Key in RandomItemPatches.HeroRelicLookup.Keys) {
                    StateVariable.GetStateVariableByName(RandomItemPatches.HeroRelicLookup[Key].Flag).BoolValue = Inventory.GetItemByName(Key).Quantity == 1;
                }
            } else if (SceneName == "TitleScreen") {
                SpeedrunTimerDisplay.Visible = TunicRandomizer.Settings.TimerOverlayEnabled;
                SpeedrunTimerDisplay.instance.timerText.text = "00:00:00.00";
                SpeedrunTimerDisplay.instance.sceneText.text = "";
                SpeedrunTimerDisplay.instance.timerText.transform.position = new Vector3(-454.1f, 245.4f, -197.0f);
                SpeedrunTimerDisplay.instance.timerText.fontSize = 64;
            } else {
                foreach (string Key in RandomItemPatches.FairyLookup.Keys) {
                    StateVariable.GetStateVariableByName(RandomItemPatches.FairyLookup[Key].Flag).BoolValue = SaveFile.GetInt("randomizer opened fairy chest " + Key) == 1;
                }
                for (int i = 0; i < 28; i++) {
                    SaveFile.SetInt("unlocked page " + i, SaveFile.GetInt("randomizer picked up page " + i) == 1 ? 1 : 0);
                }
                foreach (string Key in RandomItemPatches.HeroRelicLookup.Keys) {
                    StateVariable.GetStateVariableByName(RandomItemPatches.HeroRelicLookup[Key].Flag).BoolValue = SaveFile.GetInt("randomizer picked up " + RandomItemPatches.HeroRelicLookup[Key].OriginalPickupLocation) == 1;
                }
            } 
        }

        public static void PauseMenu___button_ReturnToTitle_PostfixPatch(PauseMenu __instance) {
            SceneName = "TitleScreen";
        }
    }
}
