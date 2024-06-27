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

        public static Dictionary<string, int> SphereZero = new Dictionary<string, int>();

        // set this to true to test location access
        public static bool testLocations = false;
        // leave this one alone
        public static bool testBool = false;

        // essentially fake items for the purpose of logic
        public static List<string> PrecollectedItems = new List<string>();

        public static List<string> LadderItems = ItemLookup.Items.Where(item => item.Value.Type == ItemTypes.LADDER).Select(item => item.Value.Name).ToList();

        public static void PopulatePrecollected()
        {
            PrecollectedItems.Clear();
            if (SaveFile.GetInt(SaveFlags.LadderRandoEnabled) == 0)
            {
                PrecollectedItems.AddRange(LadderItems);
            }
            if (SaveFile.GetInt(SaveFlags.MasklessLogic) == 1)
            {
                PrecollectedItems.Add("Mask");
            }
            if (SaveFile.GetInt(SaveFlags.LanternlessLogic) == 1)
            {
                PrecollectedItems.Add("Lantern");
            }
            if (SaveFile.GetInt(SaveFlags.AbilityShuffle) == 0)
            {
                PrecollectedItems.AddRange(new List<string> { "12", "21", "26" });
            }
        }

        public static void RandomizeAndPlaceItems(System.Random random = null) {
            if (random == null)
            {
                random = new System.Random(SaveFile.GetInt("seed"));
            }

            Locations.RandomizedLocations.Clear();
            Locations.CheckedLocations.Clear();
            PopulatePrecollected();
            List<string> Ladders = new List<string>(LadderItems);
            List<Check> InitialItems = JsonConvert.DeserializeObject<List<Check>>(ItemListJson.ItemList);
            List<Reward> InitialRewards = new List<Reward>();
            List<Location> InitialLocations = new List<Location>();
            List<Check> Hexagons = new List<Check>();
            Check Laurels = new Check();

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
                    if (Item.Location.Requirements.Count > 0 && Item.Location.Requirements.Where(dict => dict.ContainsKey("Mask") || dict.ContainsKey("Lantern")).Count() > 0) {
                        for (int i = 0; i < Item.Location.Requirements.Count; i++) {
                            if (Item.Location.Requirements[i].ContainsKey("Mask") && SaveFile.GetInt(SaveFlags.MasklessLogic) == 1) {
                                Item.Location.Requirements[i].Remove("Mask");
                            }
                            if (Item.Location.Requirements[i].ContainsKey("Lantern") && SaveFile.GetInt(SaveFlags.LanternlessLogic) == 1) {
                                Item.Location.Requirements[i].Remove("Lantern");
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
                        // todo: rewrite this to not modify the itemlistjson, and instead remove abilities as hexes get placed
                        if (SaveFile.GetInt("randomizer shuffled abilities") == 1)
                        {
                            if (Item.Location.Requirements.Count > 0)
                            {
                                for (int i = 0; i < Item.Location.Requirements.Count; i++)
                                {
                                    if (Item.Location.Requirements[i].ContainsKey("12") && Item.Location.Requirements[i].ContainsKey("21"))
                                    {
                                        int amt = Math.Max(SaveFile.GetInt($"randomizer hexagon quest prayer requirement"), SaveFile.GetInt($"randomizer hexagon quest holy cross requirement"));
                                        Item.Location.Requirements[i].Remove("12");
                                        Item.Location.Requirements[i].Remove("21");
                                        Item.Location.Requirements[i].Add("Hexagon Gold", amt);
                                    }
                                    if (Item.Location.Requirements[i].ContainsKey("12"))
                                    {
                                        Item.Location.Requirements[i].Remove("12");
                                        Item.Location.Requirements[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest prayer requirement"));
                                    }
                                    if (Item.Location.Requirements[i].ContainsKey("21"))
                                    {
                                        Item.Location.Requirements[i].Remove("21");
                                        Item.Location.Requirements[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest holy cross requirement"));
                                    }
                                    if (Item.Location.Requirements[i].ContainsKey("26"))
                                    {
                                        Item.Location.Requirements[i].Remove("26");
                                        Item.Location.Requirements[i].Add("Hexagon Gold", SaveFile.GetInt($"randomizer hexagon quest icebolt requirement"));
                                    }
                                }
                            }
                        }
                    }

                    if (SaveFile.GetInt(SaveFlags.LadderRandoEnabled) == 1 && ItemLookup.FillerItems.ContainsKey(Item.Reward.Name) && Ladders.Count > 0)
                    {
                        Item.Reward.Name = Ladders[random.Next(Ladders.Count)];
                        Item.Reward.Amount = 1;
                        Item.Reward.Type = "INVENTORY";
                        Ladders.Remove(Item.Reward.Name);
                    }

                    //Add to the rewards and location pools
                    InitialRewards.Add(Item.Reward);
                    InitialLocations.Add(Item.Location);
                }
            }

            //seeding for the portal gen ensures we get a deterministic result, still allowing re-shuffles to get new room arrangements
            int portalSeed = random.Next(int.MaxValue);
            TunicPortals.RandomizePortals(portalSeed);

            // shuffle most rewards and locations
            Shuffle(InitialRewards, InitialLocations, random);

            for (int i = 0; i < InitialRewards.Count; i++) {
                string DictionaryId = $"{InitialLocations[i].LocationId} [{InitialLocations[i].SceneName}]";
                Check Check = new Check(InitialRewards[i], InitialLocations[i]);
                Locations.RandomizedLocations.Add(DictionaryId, Check);
                TunicLogger.LogInfo("[AlternateItemRandomizer] Item Placed: " + Check.Reward.Name + " @ " + DictionaryId);
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
                    TunicLogger.LogInfo("[AlternateItemRandomizer] Hexagon Placed: " + Hexagon.Reward.Name + " @ " + DictionaryId);
                }
            }

            //re-insert laurels
            if (SaveFile.GetInt("randomizer laurels location") != 0) {
                string DictionaryId = $"{Laurels.Location.LocationId} [{Laurels.Location.SceneName}]";
                Locations.RandomizedLocations.Add(DictionaryId, Laurels);
                TunicLogger.LogInfo("[AlternateItemRandomizer] Laurels Placed: " + Laurels.Reward.Name + " @ " + DictionaryId);
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
                    TunicLogger.LogInfo("[AlternateItemRandomizer] Vanilla Item Placed: " + item.Reward.Name + " @ " + DictionaryId);
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
            Portal heirPortal = null;

            Dictionary<string, Check> RandomizedLocations = Locations.RandomizedLocations;
            Dictionary<string, int> inventory = new Dictionary<string, int>();
            List<string> checkedLocations = new List<string>();

            TunicLogger.LogInfo("[Passcheck] Started");

            inventory.Add("Overworld", 1);

            if (SaveFile.GetInt(SaveFlags.EntranceRando) == 1)
            {
                SphereZero = GetERSphereOne();
            }
            else
            {
                SphereZero = GetSphereOne();
            }

            foreach (KeyValuePair<string, int> SphereZeroItem in SphereZero)
            {
                if (!inventory.ContainsKey(SphereZeroItem.Key))
                {
                    inventory.Add(SphereZeroItem.Key, SphereZeroItem.Value);
                }
            }

            //get sphere one (locations) as well as heir portal location directly if in ER
            if (SaveFile.GetInt("randomizer entrance rando enabled") == 1)
            {
                TunicLogger.LogInfo("[Passcheck] Loading items for sphere one for entrance rando");
                Dictionary<string,int> sphere_one_list = GetERSphereOne();
                inventory.Clear();
                foreach (KeyValuePair<string,int> sphere_one_item in sphere_one_list)
                {
                    if (!inventory.ContainsKey(sphere_one_item.Key))
                    {
                        inventory.Add(sphere_one_item.Key, sphere_one_item.Value);
                    }
                }

                foreach (PortalCombo portalCombo in TunicPortals.RandomizedPortals.Values)
                {
                    if (portalCombo.Portal1.Scene == "Spirit Arena")
                    {
                        heirPortal = portalCombo.Portal2;
                        break;
                    }
                    if (portalCombo.Portal2.Scene == "Spirit Arena")
                    {
                        heirPortal = portalCombo.Portal1;
                        break;
                    }
                }
                if (heirPortal != null)
                {
                    TunicLogger.LogInfo($"[Passcheck] Heir portal found at {heirPortal.Name} in {heirPortal.Region}");
                }
            }

            TunicLogger.LogInfo("[Passcheck] Starting Inventory Filled");

            while (newLocationsThisLoop > 0 && winConditionMet == false)
            {
                loopCount++;
                newLocationsThisLoop = 0;

                TunicLogger.LogInfo("[Passcheck] Beginning Loop " + loopCount.ToString());

                foreach (string Key in RandomizedLocations.Keys)
                {

                    Reward currentReward = RandomizedLocations[Key].Reward;
                    Location currentLocation = RandomizedLocations[Key].Location;
                    string locId = currentLocation.LocationId;

                    if (!checkedLocations.Contains(currentLocation.LocationId) && currentLocation.reachable(inventory))
                    {

                        TunicLogger.LogInfo("[Passcheck] Obtained: " + currentReward.Name + " @ " + currentLocation.SceneName + currentLocation.Position);
                        if (!inventory.ContainsKey(currentLocation.SceneName)) inventory.Add(currentLocation.SceneName, 1);

                        checkedLocations.Add(locId);
                        newLocationsThisLoop++;

                        string itemName = ItemLookup.FairyLookup.Keys.Contains(currentReward.Name) ? "Fairy" : currentReward.Name;
                        addToInventory(inventory, itemName, currentReward.Amount);

                        //if questagon hunt, check if we've hit a questagon milestone and add the right progression item
                        if (SaveFile.GetInt(SaveFlags.HexagonQuestEnabled) == 1 && inventory.ContainsKey("Hexagon Gold"))
                        {
                            if (inventory["Hexagon Gold"] == SaveFile.GetInt($"randomizer hexagon quest prayer requirement") && !inventory.ContainsKey("12"))
                            {
                                inventory.Add("12", 1);
                                TunicLogger.LogInfo("[Passcheck] Got enough Questagons for Prayer");
                                newLocationsThisLoop++;

                            }

                            if (inventory["Hexagon Gold"] == SaveFile.GetInt($"randomizer hexagon quest holy cross requirement") && !inventory.ContainsKey("21"))
                            {
                                inventory.Add("21", 1);
                                TunicLogger.LogInfo("[Passcheck] Got enough Questagons for Holy Cross");
                                newLocationsThisLoop++;
                            }

                            if (inventory["Hexagon Gold"] == SaveFile.GetInt($"randomizer hexagon quest icebolt requirement") && !inventory.ContainsKey("26"))
                            {
                                inventory.Add("26", 1);
                                TunicLogger.LogInfo("[Passcheck] Got enough Questagons for Icebolt");
                                newLocationsThisLoop++;
                            }
                        }


                    }
                }

                //Update reachable regions in the inventory before we loop again
                if (SaveFile.GetInt("randomizer entrance rando enabled") == 1)
                {
                    TunicLogger.LogInfo("[Passcheck] Updating reachable regions for entrance rando");
                    inventory = TunicPortals.UpdateReachableRegions(inventory);
                }

                //check for win condition
                if (checkedLocations.Count == RandomizedLocations.Count)
                {
                    winConditionMet = true; //we can reach every check!
                } else
                {
                    winConditionMet = checkWinCondition(inventory, heirPortal);
                }

                TunicLogger.LogInfo($"[Passcheck] Ending loop {loopCount}, found {newLocationsThisLoop} new checks this time around!");
            }

            if (!winConditionMet)
            {
                TunicLogger.LogInfo("[Passcheck] Unable to find a win condition for this seed!");
            }
            return winConditionMet;

        }

        public static List<string> findPortalPathToItem(string itemName)
        {
            if (SaveFile.GetInt("randomizer entrance rando enabled") == 1)
            {
                TunicLogger.LogInfo("Can't use findPortalPathToItem outside of ER");
                return new List<string>();
            }
            return new List<string>();
            //get a list of the currently-accessible locations.  any portals leading back to one of these completes the path
            //find the region the desired reward is attached to
            //for each portal pair in the region:
            //  skip this item if the portal pairing has already been traversed
            //  add the portal pairing to those that have already been traversed
            //  add the requirements for this portal pairing to a list for this chain (if it doesn't already exist)
            //  Check if the other end of the pairing leads back to one of our currently-accessible locations
            //      If so, return the full chain of required items!
            //      If not, call this on the region on the other end of the portal

        }

        public static bool checkWinCondition(Dictionary<string, int> inventory, Portal heirPortal = null)
        {
            TunicLogger.LogInfo("[Passcheck] Checking for a win condition");
            if (SaveFile.GetInt("randomizer entrance rando enabled") == 1)
            {
                if (heirPortal == null)
                {
                    TunicLogger.LogInfo("[Passcheck] ---- ERROR Entrance rando is enabled but the heir portal can't be found! ");
                    return false;

                }
                if (inventory.ContainsKey(heirPortal.Region))
                {
                    TunicLogger.LogInfo($"[Passcheck] ---- PASS heir portal in {heirPortal.Region} region is reachable!");
                } else
                {
                    TunicLogger.LogInfo($"[Passcheck] ---- FAIL heir portal in {heirPortal.Region} region is not yet reachable!");
                    return false;
                }
            } else
            {
                if (SaveFile.GetInt("randomizer entrance rando enabled") == 0 && inventory.ContainsKey("12"))
                {
                    TunicLogger.LogInfo($"[Passcheck] ---- PASS Got prayer, the Far Shore is reachable!");
                } else
                {
                    TunicLogger.LogInfo($"[Passcheck] ---- FAIL No prayer, the Far Shore is un-reachable!");
                    return false;
                }
            }


            if (SaveFile.GetInt(SaveFlags.HexagonQuestEnabled) == 1)
            {
                if (inventory.ContainsKey("Hexagon Gold") && inventory["Hexagon Gold"] >= SaveFile.GetInt("randomizer hexagon quest goal"))
                {
                    TunicLogger.LogInfo("[Passcheck] ---- PASS Got enough gold questagons!");
                    TunicLogger.LogInfo("[Passcheck] Questagon Hunt win condition found!");
                    return true;
                } else
                {
                    TunicLogger.LogInfo("[Passcheck] ---- FAIL Not enough gold questagons!");
                    return false;
                }
            }
            else
            {
                if (inventory.ContainsKey("Hexagon Red") && inventory.ContainsKey("Hexagon Green") && inventory.ContainsKey("Hexagon Blue")) //access to sealed temple to insert questagons)
                {
                    TunicLogger.LogInfo("[Passcheck] ---- PASS Got all three questagons!");
                }
                else
                {
                    TunicLogger.LogInfo("[Passcheck] ---- FAIL don't have all 3 questagons!");
                    return false;
                }

                if ((inventory.ContainsKey("Hyperdash") || inventory.ContainsKey("Lantern")))
                {
                    TunicLogger.LogInfo("[Passcheck] ---- PASS Got access to Sealed Temple!");
                    TunicLogger.LogInfo("[Passcheck] Standard Logic win condition found!");
                    return true;
                } else
                {
                    TunicLogger.LogInfo("[Passcheck] ---- FAIL No access to Sealed Temple!");
                    return false;
                }

            }
        }

        private static void Shuffle(List<Reward> Rewards, List<Location> Locations, System.Random random)
        {
            int n = Rewards.Count;
            int r;
            int l;
            while (n > 1)
            {
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

        private static void Shuffle(List<Check> list, System.Random random)
        {
            int n = list.Count;
            int r;
            while (n > 1)
            {
                n--;
                r = random.Next(n + 1);

                Check holder = list[r];
                list[r] = list[n];
                list[n] = holder;
            }
        }

        // add a key if it doesn't exist, otherwise increment the value by 1
        public static Dictionary<string, int> AddListToDict(Dictionary<string, int> dictionary, List<string> list)
        {
            foreach (string item in list)
            {
                dictionary.TryGetValue(item, out var count);
                dictionary[item] = count + 1;
            }
            return dictionary;
        }

        public static Dictionary<string, int> AddStringToDict(Dictionary<string, int> dictionary, string item)
        {
            dictionary.TryGetValue(item, out var count);
            dictionary[item] = count + 1;
            return dictionary;
        }

        public static Dictionary<string, int> AddDictToDict(Dictionary<string, int> dictionary1, Dictionary<string, int> dictionary2)
        {
            foreach (KeyValuePair<string, int> pair in dictionary2)
            {
                dictionary1.TryGetValue(pair.Key, out var count);
                dictionary1[pair.Key] = count + pair.Value;
            }
            return dictionary1;
        }

        public static Check FindRandomizedItemByName(string Name)
        {
            foreach (Check Check in Locations.RandomizedLocations.Values)
            {
                if (Check.Reward.Name == Name)
                {
                    return Check;
                }
            }
            return null;
        }

        public static List<Check> FindAllRandomizedItemsByName(string Name)
        {
            List<Check> results = new List<Check>();

            foreach (Check Check in Locations.RandomizedLocations.Values)
            {
                if (Check.Reward.Name == Name)
                {
                    results.Add(Check);
                }
            }

            return results;
        }

        public static List<Check> FindAllRandomizedItemsByType(string type)
        {
            List<Check> results = new List<Check>();

            foreach (Check Check in Locations.RandomizedLocations.Values)
            {
                if (Check.Reward.Type == type)
                {
                    results.Add(Check);
                }
            }

            return results;
        }

        // in non-ER, we want the actual sphere 1
        public static Dictionary<string, int> GetSphereOne(Dictionary<string, int> startInventory = null)
        {
            Dictionary<string, int> Inventory = new Dictionary<string, int>() { { "Overworld", 1 } };
            Dictionary<string, PortalCombo> vanillaPortals = TunicPortals.VanillaPortals();
            if (startInventory == null)
            {
                AddListToDict(Inventory, PrecollectedItems);
            }
            else
            {
                AddDictToDict(Inventory, startInventory);
            }

            while (true)
            {
                int start_num = Inventory.Count;
                Inventory = TunicPortals.UpdateReachableRegions(Inventory);
                foreach (PortalCombo portalCombo in vanillaPortals.Values)
                {
                    Inventory = portalCombo.AddComboRegions(Inventory);
                }
                int end_num = Inventory.Count;
                if (start_num == end_num)
                {
                    break;
                }
            }
            return Inventory;
        }

        // In ER, we want sphere 1 to be in Overworld or adjacent to Overworld
        public static Dictionary<string, int> GetERSphereOne(Dictionary<string, int> startInventory = null)
        {
            List<Portal> PortalInventory = new List<Portal>();
            Dictionary<string, int> Inventory = new Dictionary<string, int>() { { "Overworld", 1 } };

            if (startInventory == null)
            {
                AddListToDict(Inventory, PrecollectedItems);
            }
            else
            {
                AddDictToDict(Inventory, startInventory);
            }

            Inventory = TunicPortals.FirstStepsUpdateReachableRegions(Inventory);

            // find which portals you can reach from spawn without additional progression
            foreach (PortalCombo portalCombo in TunicPortals.RandomizedPortals.Values)
            {
                if (Inventory.ContainsKey(portalCombo.Portal1.Region))
                {
                    PortalInventory.Add(portalCombo.Portal2);
                }
                if (Inventory.ContainsKey(portalCombo.Portal2.Region))
                {
                    PortalInventory.Add(portalCombo.Portal1);
                }
            }

            // add the regions you can reach as your first steps to the inventory
            foreach (Portal portal in PortalInventory)
            {
                if (!Inventory.ContainsKey(portal.Region))
                {
                    Inventory.Add(portal.Region, 1);
                }
            }
            Inventory = TunicPortals.FirstStepsUpdateReachableRegions(Inventory);
            return Inventory;
        }


        public static void addToInventory(Dictionary<string, int> inventory, string itemName, int amount = 1)
        {
            if (inventory.ContainsKey(itemName))
            {
                inventory[itemName] += amount;
            }
            else
            {
                inventory.Add(itemName, amount);
            }
        }
    }
}

