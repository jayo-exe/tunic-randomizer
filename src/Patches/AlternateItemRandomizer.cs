using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using UnityEngine.InputSystem.Utilities;
using Newtonsoft.Json;

namespace TunicRandomizer {
    public class AlternateItemRandomizer {
        private static ManualLogSource Logger = TunicRandomizer.Logger;

        public static Dictionary<string, int> SphereZero = new Dictionary<string, int>();

        public static void PopulateSphereZero() {
            SphereZero.Clear();
            if (SaveFile.GetInt("randomizer shuffled abilities") == 0) {
                SphereZero.Add("12", 1);
                SphereZero.Add("21", 1);
                SphereZero.Add("26", 1);
            }
            if (SaveFile.GetInt("randomizer started with sword") == 1) {
                SphereZero.Add("Sword", 1);
            }
        }

        public static void RandomizeAndPlaceItems(System.Random random = null) {
            if(random == null)
            {
                random = new System.Random(SaveFile.GetInt("seed"));
            }
            
            Locations.RandomizedLocations.Clear();
            Locations.CheckedLocations.Clear();

            List<Check> InitialItems = JsonConvert.DeserializeObject<List<Check>>(ItemListJson.ItemList);
            List<Reward> InitialRewards = new List<Reward>();
            List<Location> InitialLocations = new List<Location>();
            List<Check> Hexagons = new List<Check>();
            Check Laurels = new Check();
            Dictionary<string, int> SphereZeroInventory = new Dictionary<string, int>(SphereZero);
            int GoldHexagonsAdded = 0;
            int HexagonsToAdd = (int)Math.Round((100f + SaveFile.GetInt("randomizer hexagon quest extras")) / 100f * SaveFile.GetInt("randomizer hexagon quest goal"));
            
            if (SaveFile.GetInt(SaveFlags.HexagonQuestEnabled) == 1 && SaveFile.GetInt("randomizer shuffled abilities") == 1) {
                int HexGoal = SaveFile.GetInt("randomizer hexagon quest goal");
                List<string> abilities = new List<string>() { "prayer", "holy cross", "icebolt" }.OrderBy(r => random.Next()).ToList();
                List<int> ability_unlocks = new List<int>() { (int)(HexGoal / 4f), (int)((HexGoal / 4f) * 2), (int)((HexGoal / 4f) * 3) }.OrderBy(r => random.Next()).ToList();
                for (int i = 0; i < 3; i++) {
                    int index = random.Next(abilities.Count);
                    int index2 = random.Next(ability_unlocks.Count);
                    SaveFile.SetInt($"randomizer hexagon quest {abilities[index]} requirement", ability_unlocks[index2]);
                    abilities.RemoveAt(index);
                    ability_unlocks.RemoveAt(index2);
                }
            }
            
            Shuffle(InitialItems, random);
            
            foreach (Check Item in InitialItems) {

                if (SaveFile.GetInt(SaveFlags.MasklessLogic) == 1 || SaveFile.GetInt(SaveFlags.LanternlessLogic) == 1) {
                    //If we're maskless and/or lanternless, remove these requirements from the location
                    if (Item.Location.RequiredItems.Count > 0 && Item.Location.RequiredItems.Where(dict => dict.ContainsKey("Mask") || dict.ContainsKey("Lantern")).Count() > 0) {
                        for (int i = 0; i < Item.Location.RequiredItems.Count; i++) {
                            if (Item.Location.RequiredItems[i].ContainsKey("Mask") && SaveFile.GetInt(SaveFlags.MasklessLogic) == 1) {
                                Item.Location.RequiredItems[i].Remove("Mask");
                            }
                            if (Item.Location.RequiredItems[i].ContainsKey("Lantern") && SaveFile.GetInt(SaveFlags.LanternlessLogic) == 1) {
                                Item.Location.RequiredItems[i].Remove("Lantern");
                            }
                        }
                    }
                }


                if (SaveFile.GetInt("randomizer keys behind bosses") != 0 && (Item.Reward.Name.Contains("Hexagon") || Item.Reward.Name == "Vault Key (Red)")) {
                    //if we're planning placment of hexagons, pull them from the pool, and swap the red questagon directly onto the boss
                    if (Item.Reward.Name == "Hexagon Green" || Item.Reward.Name == "Hexagon Blue") {
                        Hexagons.Add(Item);
                    } else if (Item.Reward.Name == "Vault Key (Red)") {
                        Item.Reward.Name = "Hexagon Red";
                        Hexagons.Add(Item);
                    } else if (Item.Reward.Name == "Hexagon Red") {
                        Item.Reward.Name = "Vault Key (Red)";
                        InitialRewards.Add(Item.Reward);
                        InitialLocations.Add(Item.Location);
                    }
                } else if ((SaveFile.GetInt("randomizer laurels location") == 1 && Item.Location.LocationId == "Well Reward (6 Coins)")
                    || (SaveFile.GetInt("randomizer laurels location") == 2 && Item.Location.LocationId == "Well Reward (10 Coins)")
                    || (SaveFile.GetInt("randomizer laurels location") == 3 && Item.Location.LocationId == "waterfall")) {
                    //If we're planning the laurels check, extract the location when we find it
                    InitialRewards.Add(Item.Reward);
                    Laurels.Location = Item.Location;
                } else if (SaveFile.GetInt("randomizer laurels location") != 0 && Item.Reward.Name == "Hyperdash") {
                    //If we're planning the laurels check, extract the reward when we find it
                    InitialLocations.Add(Item.Location);
                    Laurels.Reward = Item.Reward;
                } else {
                    if (SaveFile.GetInt("randomizer sword progression enabled") != 0 && (Item.Reward.Name == "Stick" || Item.Reward.Name == "Sword" || Item.Location.LocationId == "5")) {
                        //Swap out sword items for progressive ones
                        Item.Reward.Name = "Sword Progression";
                        Item.Reward.Type = "SPECIAL";
                    }
                    if (SaveFile.GetInt(SaveFlags.HexagonQuestEnabled) == 1) {
                        if (Item.Reward.Type == "PAGE" || Item.Reward.Name.Contains("Hexagon")) {
                            //Replace questagons and pages with filler
                            string FillerItem = ItemLookup.FillerItems.Keys.ToList()[random.Next(ItemLookup.FillerItems.Count)];
                            Item.Reward.Name = FillerItem;
                            Item.Reward.Type = FillerItem == "money" ? "MONEY" : "INVENTORY";
                            Item.Reward.Amount = ItemLookup.FillerItems[FillerItem][random.Next(ItemLookup.FillerItems[FillerItem].Count)];
                        }
                        if (ItemLookup.FillerItems.ContainsKey(Item.Reward.Name) && ItemLookup.FillerItems[Item.Reward.Name].Contains(Item.Reward.Amount) && GoldHexagonsAdded < HexagonsToAdd) {
                            //Replace fillter items with a gold questagon if we dont have enough yet
                            Item.Reward.Name = "Hexagon Gold";
                            Item.Reward.Type = "SPECIAL";
                            Item.Reward.Amount = 1;
                            GoldHexagonsAdded++;
                        }
                        if (SaveFile.GetInt("randomizer shuffled abilities") == 1) {
                            if (Item.Location.RequiredItems.Count > 0) {
                                for (int i = 0; i < Item.Location.RequiredItems.Count; i++) {
                                    //if a check needs one or more questagon abilities, remove them from the location and replace them with the needed questagon count
                                    if (Item.Location.RequiredItems[i].ContainsKey("12") && Item.Location.RequiredItems[i].ContainsKey("21")) {
                                        int amt = Math.Max(SaveFile.GetInt($"randomizer hexagon quest prayer requirement"), SaveFile.GetInt($"randomizer hexagon quest holy cross requirement"));
                                        Item.Location.RequiredItems[i].Remove("12");
                                        Item.Location.RequiredItems[i].Remove("21");
                                        Item.Location.RequiredItems[i].Add("Hexagon Gold", amt);
                                    }
                                    if (Item.Location.RequiredItems[i].ContainsKey("12")) {
                                        Item.Location.RequiredItems[i].Remove("12");
                                        Item.Location.RequiredItems[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest prayer requirement"));
                                    }
                                    if (Item.Location.RequiredItems[i].ContainsKey("21")) {
                                        Item.Location.RequiredItems[i].Remove("21");
                                        Item.Location.RequiredItems[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest holy cross requirement"));
                                    }
                                    if (Item.Location.RequiredItems[i].ContainsKey("26")) {
                                        Item.Location.RequiredItems[i].Remove("26");
                                        Item.Location.RequiredItems[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest icebolt requirement"));
                                    }
                                }
                            }
                            if (Item.Location.RequiredItemsDoors.Count > 0) {
                                for (int i = 0; i < Item.Location.RequiredItemsDoors.Count; i++) {
                                    //if a door check needs one or more questagon abilities, remove them from the location and replace them with the needed questagon count
                                    if (Item.Location.RequiredItemsDoors[i].ContainsKey("12") && Item.Location.RequiredItemsDoors[i].ContainsKey("21")) {
                                        int amt = Math.Max(SaveFile.GetInt($"randomizer hexagon quest prayer requirement"), SaveFile.GetInt($"randomizer hexagon quest holy cross requirement"));
                                        Item.Location.RequiredItemsDoors[i].Remove("12");
                                        Item.Location.RequiredItemsDoors[i].Remove("21");
                                        Item.Location.RequiredItemsDoors[i].Add("Hexagon Gold", amt);
                                    }
                                    if (Item.Location.RequiredItemsDoors[i].ContainsKey("12")) {
                                        Item.Location.RequiredItemsDoors[i].Remove("12");
                                        Item.Location.RequiredItemsDoors[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest prayer requirement"));
                                    }
                                    if (Item.Location.RequiredItemsDoors[i].ContainsKey("21")) {
                                        Item.Location.RequiredItemsDoors[i].Remove("21");
                                        Item.Location.RequiredItemsDoors[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest holy cross requirement"));
                                    }
                                    if (Item.Location.RequiredItemsDoors[i].ContainsKey("26")) {
                                        Item.Location.RequiredItemsDoors[i].Remove("26");
                                        Item.Location.RequiredItemsDoors[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest icebolt requirement"));
                                    }
                                }
                            }
                        }
                    }
                    //Add to the rewards and location pools
                    InitialRewards.Add(Item.Reward);
                    InitialLocations.Add(Item.Location);
                }
            }

            TunicPortals.RandomizePortals(SaveFile.GetInt("seed"));

            // shuffle most rewards and locations
            Shuffle(InitialRewards, InitialLocations, random);

            for (int i = 0; i < InitialRewards.Count; i++) {
                string DictionaryId = $"{InitialLocations[i].LocationId} [{InitialLocations[i].SceneName}]";
                Check Check = new Check(InitialRewards[i], InitialLocations[i]);
                Locations.RandomizedLocations.Add(DictionaryId, Check);
                Logger.LogInfo("[AlternateItemRandomizer] Item Placed: " + Check.Reward.Name + " @ " + DictionaryId);
            }


            //re-inset keys behind bosses
            if (SaveFile.GetInt("randomizer keys behind bosses") != 0) {
                foreach (Check Hexagon in Hexagons) {
                    if (SaveFile.GetInt(SaveFlags.HexagonQuestEnabled) == 1) {
                        Hexagon.Reward.Name = "Hexagon Gold";
                        Hexagon.Reward.Type = "SPECIAL";
                    }
                    string DictionaryId = $"{Hexagon.Location.LocationId} [{Hexagon.Location.SceneName}]";
                    Locations.RandomizedLocations.Add(DictionaryId, Hexagon);
                    Logger.LogInfo("[AlternateItemRandomizer] Hexagon Placed: " + Hexagon.Reward.Name + " @ " + DictionaryId);
                }
            }

            //re-insert laurels
            if (SaveFile.GetInt("randomizer laurels location") != 0) {
                string DictionaryId = $"{Laurels.Location.LocationId} [{Laurels.Location.SceneName}]";
                Locations.RandomizedLocations.Add(DictionaryId, Laurels);
                Logger.LogInfo("[AlternateItemRandomizer] Laurels Placed: " + Laurels.Reward.Name + " @ " + DictionaryId);
            }

            //wipe this all out if we're playing vanilla
            if (SaveFile.GetString("randomizer game mode") == "VANILLA")
            {
                Locations.RandomizedLocations.Clear();
                foreach (Check item in JsonConvert.DeserializeObject<List<Check>>(ItemListJson.ItemList))
                {
                    if (SaveFile.GetInt("randomizer sword progression enabled") != 0)
                    {
                        if (item.Reward.Name == "Stick" || item.Reward.Name == "Sword" || item.Location.LocationId == "5")
                        {
                            item.Reward.Name = "Sword Progression";
                            item.Reward.Type = "SPECIAL";
                        }
                    }
                    string DictionaryId = $"{item.Location.LocationId} [{item.Location.SceneName}]";
                    Locations.RandomizedLocations.Add(DictionaryId, item);
                    Logger.LogInfo("[AlternateItemRandomizer] Vanilla Item Placed: " + item.Reward.Name + " @ " + DictionaryId);
                }
            }

            //replace money checks with traps
            foreach (string key in Locations.RandomizedLocations.Keys.ToList()) {
                Check check = Locations.RandomizedLocations[key];
                if (check.Reward.Type == "MONEY") {
                    if ((TunicRandomizer.Settings.FoolTrapIntensity == RandomizerSettings.FoolTrapOption.NORMAL && check.Reward.Amount < 20)
                    || (TunicRandomizer.Settings.FoolTrapIntensity == RandomizerSettings.FoolTrapOption.DOUBLE && check.Reward.Amount <= 20)
                    || (TunicRandomizer.Settings.FoolTrapIntensity == RandomizerSettings.FoolTrapOption.ONSLAUGHT && check.Reward.Amount <= 30)) {
                        check.Reward.Name = "Fool Trap";
                        check.Reward.Type = "FOOL";
                    }
                }
            }

            //loop back around if the seed isn't beatable
            if (!isSeedBeatable())
            {
                RandomizeAndPlaceItems(random);
                return;
            }

            //build up tracker data
            foreach (string Key in Locations.RandomizedLocations.Keys) {
                int ItemPickedUp = SaveFile.GetInt($"randomizer picked up {Key}");
                Locations.CheckedLocations.Add(Key, ItemPickedUp == 1 ? true : false);
            }
            if (TunicRandomizer.Tracker.ItemsCollected.Count == 0) {
                foreach (KeyValuePair<string, bool> PickedUpItem in Locations.CheckedLocations.Where(item => item.Value)) {
                    Check check = Locations.RandomizedLocations[PickedUpItem.Key];
                    ItemData itemData = ItemLookup.GetItemDataFromCheck(check);
                    TunicRandomizer.Tracker.SetCollectedItem(itemData.Name, false);
                }
                ItemTracker.SaveTrackerFile();
                TunicRandomizer.Tracker.ImportantItems["Flask Container"] += TunicRandomizer.Tracker.ItemsCollected.Where(Item => Item.Name == "Flask Shard").Count() / 3;
                if (SaveFile.GetInt("randomizer started with sword") == 1) {
                    TunicRandomizer.Tracker.ImportantItems["Sword"] += 1;
                }
            }
        }

        

        public static bool isSeedBeatable()
        {
            int loopCount = 0;
            int newLocationsThisLoop = 1;
            bool winConditionMet = false;

            Dictionary<string, Check> RandomizedLocations = Locations.RandomizedLocations;
            Dictionary<string, int> inventory = new Dictionary<string, int>();
            List<string> checkedLocations = new List<string>();

            Logger.LogInfo("[Passcheck] Started");

            inventory.Add("Overworld", 1);
            foreach(KeyValuePair<string, int> SphereZeroItem in SphereZero)
            {
                if (!inventory.ContainsKey(SphereZeroItem.Key))
                {
                    inventory.Add(SphereZeroItem.Key, SphereZeroItem.Value);
                }
            }

            if (SaveFile.GetInt("randomizer entrance rando enabled") == 1)
            {
                Logger.LogInfo("[Passcheck] Loading items for sphere zero for entrance rando");
                List<string> sphere_zero_list = GetERSphereOne();
                inventory.Clear();
                foreach (string sphere_zero_item in sphere_zero_list)
                {
                    if (!inventory.ContainsKey(sphere_zero_item))
                    {
                        inventory.Add(sphere_zero_item, 1);
                    }
                }
            }

            Logger.LogInfo("[Passcheck] Sphere Zero Filled");

            while (newLocationsThisLoop > 0 && winConditionMet == false)
            {
                loopCount++;
                newLocationsThisLoop = 0;
                

                Logger.LogInfo("[Passcheck] Beginning Loop " + loopCount.ToString());

                //Update reachable regions in the inventory before we loop again
                if (SaveFile.GetInt("randomizer entrance rando enabled") == 1)
                {
                    Logger.LogInfo("[Passcheck] Updating reachable regions for entrance rando");
                    inventory = TunicPortals.UpdateReachableRegions(inventory);
                }

                foreach (string Key in RandomizedLocations.Keys)
                {
                    string locId = RandomizedLocations[Key].Location.LocationId;

                    if (!checkedLocations.Contains(locId) && RandomizedLocations[Key].Location.reachable(inventory))
                    {

                        Logger.LogInfo("[Passcheck] Obtained: " + RandomizedLocations[Key].Reward.Name + " @ " + RandomizedLocations[Key].Location.SceneName  + RandomizedLocations[Key].Location.Position);

                        checkedLocations.Add(locId);
                        newLocationsThisLoop++;

                        string itemName = ItemLookup.FairyLookup.Keys.Contains(RandomizedLocations[Key].Reward.Name) ? "Fairy" : RandomizedLocations[Key].Reward.Name;
                        if (inventory.ContainsKey(itemName))
                        {
                            inventory[itemName] += RandomizedLocations[Key].Reward.Amount;
                        }
                        else
                        {
                            inventory.Add(itemName, RandomizedLocations[Key].Reward.Amount);
                        }

                        //if questagon hunt, check if we've hit a questagon milestone and add the right progression item
                        if (SaveFile.GetInt(SaveFlags.HexagonQuestEnabled) == 1 && inventory.ContainsKey("Hexagon Gold"))
                        {
                            if (inventory["Hexagon Gold"] == SaveFile.GetInt($"randomizer hexagon quest prayer requirement") && !inventory.ContainsKey("12"))
                            {
                                inventory.Add("12", 1);
                                Logger.LogInfo("[Passcheck] Got enough Questagons for Prayer");
                                newLocationsThisLoop++;

                            }

                            if (inventory["Hexagon Gold"] == SaveFile.GetInt($"randomizer hexagon quest holy cross requirement") && !inventory.ContainsKey("21"))
                            {
                                inventory.Add("21", 1);
                                Logger.LogInfo("[Passcheck] Got enough Questagons for Holy Cross");
                                newLocationsThisLoop++;
                            }

                            if (inventory["Hexagon Gold"] == SaveFile.GetInt($"randomizer hexagon quest icebolt requirement") && !inventory.ContainsKey("26"))
                            {
                                inventory.Add("26", 1);
                                Logger.LogInfo("[Passcheck] Got enough Questagons for Icebolt");
                                newLocationsThisLoop++;
                            }

                        }


                    }
                }

                //check for win condition
                if (SaveFile.GetInt(SaveFlags.HexagonQuestEnabled) == 1)
                {
                    Logger.LogInfo("[Passcheck] Checking for questagon mode win condition");
                    if (inventory.ContainsKey("Hexagon Gold") && inventory["Hexagon Gold"] >= SaveFile.GetInt("randomizer hexagon quest goal"))
                    {
                        winConditionMet = true;
                        Logger.LogInfo("[Passcheck] Questagon Hunt win condition found!");
                    }
                }
                else
                {
                    Logger.LogInfo("[Passcheck] Checking for standard mode win condition");
                    if (
                        (inventory.ContainsKey("Hexagon Red") && inventory.ContainsKey("Hexagon Green") && inventory.ContainsKey("Hexagon Blue"))
                        && (inventory.ContainsKey("Hyperdash") || inventory.ContainsKey("Lantern"))
                    )
                    {
                        winConditionMet = true;
                        Logger.LogInfo("[Passcheck] Standard Logic win condition found!");
                    }
                }

                
                Logger.LogInfo($"[Passcheck] Ending loop {loopCount}, found {newLocationsThisLoop} new checks this time around!");
            }

            if(!winConditionMet)
            {
                Logger.LogInfo("[Passcheck] Unable to find a win condition for this seed!");
            }
            return winConditionMet;
            
        }

        private static void Shuffle(List<Reward> Rewards, List<Location> Locations, System.Random random) {
            int n = Rewards.Count;
            int r;
            int l;
            while (n > 1) {
                n--;
                r = random.Next(n + 1);
                l = random.Next(n + 1);

                Reward Reward = Rewards[r];
                Rewards[r] = Rewards[n];
                Rewards[n] = Reward;

                Location Location = Locations[l];
                Locations[l] = Locations[n];
                Locations[n] = Location;
            }
        }

        private static void Shuffle(List<Check> list, System.Random random) {
            int n = list.Count;
            int r;
            while (n > 1) {
                n--;
                r = random.Next(n + 1);

                Check holder = list[r];
                list[r] = list[n];
                list[n] = holder;
            }
        }

        public static Check FindRandomizedItemByName(string Name) {
            foreach (Check Check in Locations.RandomizedLocations.Values) {
                if (Check.Reward.Name == Name) {
                    return Check;
                }
            }
            return null;
        }

        public static List<Check> FindAllRandomizedItemsByName(string Name) {
            List<Check> results = new List<Check>();

            foreach (Check Check in Locations.RandomizedLocations.Values) {
                if (Check.Reward.Name == Name) {
                    results.Add(Check);
                }
            }

            return results;
        }

        public static List<Check> FindAllRandomizedItemsByType(string type) {
            List<Check> results = new List<Check>();

            foreach (Check Check in Locations.RandomizedLocations.Values) {
                if (Check.Reward.Type == type) {
                    results.Add(Check);
                }
            }

            return results;
        }

        // In ER, we want sphere 1 to be in Overworld or adjacent to Overworld
        public static List<string> GetERSphereOne() {
            List<Portal> PortalInventory = new List<Portal>();
            List<string> CombinedInventory = new List<string> { "Overworld" };

            // add starting sword and abilities as applicable
            if (SaveFile.GetInt("randomizer started with sword") == 1) {
                CombinedInventory.Add("Sword");
            }
            if (SaveFile.GetInt("randomizer shuffled abilities") == 0) {
                CombinedInventory.Add("12");
                CombinedInventory.Add("21");
            }
            // add these too if you're ignoring them in logic
            if (SaveFile.GetInt(SaveFlags.MasklessLogic) == 1) {
                CombinedInventory.Add("Mask");
            }
            if (SaveFile.GetInt(SaveFlags.LanternlessLogic) == 1) {
                CombinedInventory.Add("Lantern");
            }
            CombinedInventory = TunicPortals.FirstStepsUpdateReachableRegions(CombinedInventory);
            
            // find which portals you can reach from spawn without additional progression
            foreach (PortalCombo portalCombo in TunicPortals.RandomizedPortals.Values) {
                if (CombinedInventory.Contains(portalCombo.Portal1.Region)) {
                    PortalInventory.Add(portalCombo.Portal2);
                }
                if (CombinedInventory.Contains(portalCombo.Portal2.Region)) {
                    PortalInventory.Add(portalCombo.Portal1);
                }
            }

            // add the regions you can reach as your first steps to the inventory
            foreach (Portal portal in PortalInventory) {
                if (!CombinedInventory.Contains(portal.Region)) {
                    CombinedInventory.Add(portal.Region);
                }
            }
            CombinedInventory = TunicPortals.FirstStepsUpdateReachableRegions(CombinedInventory);
            return CombinedInventory;
        }
    }
}
