using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.View;
using Kingmaker.View.Equipment;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Visual.Decals;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Sound;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace VisualAdjustments
{
    public class InfoManager
    {
        public class EquipmentEntityInfo
        {
            public string type = "Unknown";
            public string raceGenderCombos = "";
            public EquipmentEntityLink eel = null;
        }
        static private Dictionary<string, EquipmentEntityInfo> m_lookup = null;
        static Dictionary<string, EquipmentEntityInfo> lookup
        {
            get
            {
                if (m_lookup == null) BuildLookup();
                return m_lookup;
            }
        }
        private static UnorderedList<string, string> m_OrphanedKingmakerEquipment;
        private static string selectedKingmakerOrphanedEquipment = "";
        private static UnorderedList<string, string> m_OrphanedMaleEquipment;
        private static UnorderedList<string, string> m_OrphanedFemaleEquipment;
        private static string selectedOrphanedEquipment = "";
        static BlueprintBuff[] blueprintBuffs = new BlueprintBuff[] { };
        static bool showWeapons = false;
        static bool showCharacter = false;
        static bool showBuffs = false;
        static bool showFx = false;
        static bool showAsks = false;
        static bool showDoll = false;
        static bool showPortrait = false;
        static string GetName(EquipmentEntityLink link)
        {
            if (ResourcesLibrary.LibraryObject.ResourceNamesByAssetId.ContainsKey(link.AssetId)) return ResourcesLibrary.LibraryObject.ResourceNamesByAssetId[link.AssetId];
            return null;
        }
        static void AddLinks(EquipmentEntityLink[] links, string type, Race race, Gender gender)
        {
            foreach (var link in links)
            {
                var name = GetName(link);
                if (name == null) continue;
                if (lookup.ContainsKey(name))
                {
                    lookup[name].raceGenderCombos += ", " + race + gender;
                }
                else
                {
                    lookup[name] = new EquipmentEntityInfo
                    {
                        type = type,
                        raceGenderCombos = "" + race + gender,
                        eel = link
                    };
                }
            }
        }
        static void BuildLookup()
        {
            m_lookup = new Dictionary<string, EquipmentEntityInfo>(); ;
            var races = ResourcesLibrary.GetBlueprints<BlueprintRace>();
            var racePresets = ResourcesLibrary.GetBlueprints<BlueprintRaceVisualPreset>();
            var classes = ResourcesLibrary.GetBlueprints<BlueprintCharacterClass>();
            foreach (var race in races)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
                    AddLinks(customizationOptions.Heads, "Head", race.RaceId, gender);
                    AddLinks(customizationOptions.Hair, "Hair", race.RaceId, gender);
                    AddLinks(customizationOptions.Beards, "Beards", race.RaceId, gender);
                    AddLinks(customizationOptions.Eyebrows, "Eyebrows", race.RaceId, gender);
                }
            }
            foreach (var racePreset in racePresets)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    var raceSkin = racePreset.Skin;
                    if (raceSkin == null) continue;
                    AddLinks(raceSkin.GetLinks(gender, racePreset.RaceId), "Skin", racePreset.RaceId, gender);
                }
            }
            foreach (var _class in classes)
            {
                foreach (var race in races)
                {
                    foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                    {
                        AddLinks(_class.GetClothesLinks(gender, race.RaceId).ToArray(), "ClassOutfit", race.RaceId, gender);
                    }
                }
            }
            var gear = ResourcesLibrary.GetBlueprints<KingmakerEquipmentEntity>();
            foreach (var race in races)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    foreach (var kee in gear)
                    {
                        AddLinks(kee.GetLinks(gender, race.RaceId), "Armor", race.RaceId, gender);
                    }
                }
            }
            blueprintBuffs = ResourcesLibrary.GetBlueprints<BlueprintBuff>().ToArray();
        }
        public static void ShowInfo(UnitEntityData unitEntityData)
        {;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild Character"))
            {
                CharacterManager.RebuildCharacter(unitEntityData);
            }
            if (GUILayout.Button("Rebuild Outfit"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                unitEntityData.View.CharacterAvatar.RebuildOutfit();
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Update Class Equipment"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                bool useClassEquipment = unitEntityData.Descriptor.ForcceUseClassEquipment;
                unitEntityData.Descriptor.ForcceUseClassEquipment = true;
                unitEntityData.View.UpdateClassEquipment();
                unitEntityData.Descriptor.ForcceUseClassEquipment = useClassEquipment;
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Update Body Equipment"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                unitEntityData.View.UpdateBodyEquipmentModel();
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Update Model"))
            {
                CharacterManager.UpdateModel(unitEntityData.View);
            }
            if (GUILayout.Button("Update HandsEquipment"))
            {
                unitEntityData.View.HandsEquipment.UpdateAll();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle Stance"))
            {
                unitEntityData.View.HandsEquipment.ForceSwitch(!unitEntityData.View.HandsEquipment.InCombat);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Original size {unitEntityData.Descriptor.OriginalSize}");
            GUILayout.Label($"Current size {unitEntityData.Descriptor.State.Size}");
            var m_OriginalScale = Traverse.Create(unitEntityData.View).Field("m_OriginalScale").GetValue<Vector3>();
            var m_Scale = Traverse.Create(unitEntityData.View).Field("m_Scale").GetValue<float>();
            var realScale = unitEntityData.View.transform.localScale;
            GUILayout.Label($"View Original {m_OriginalScale.x:0.#}");
            GUILayout.Label($"View Current {m_Scale:0.#}");
            GUILayout.Label($"View Real {realScale.x:0.#}");
            GUILayout.Label($"Disabled Scaling {unitEntityData.View.DisableSizeScaling}");
            GUILayout.EndHorizontal();
            var message =
                    unitEntityData.View == null ? "No View" :
                    unitEntityData.View.CharacterAvatar == null ? "No Character Avatar" :
                    null;
            if(message != null) GUILayout.Label(message);
            GUILayout.BeginHorizontal();
            showCharacter = GUILayout.Toggle(showCharacter, "Show Character");
            showWeapons = GUILayout.Toggle(showWeapons, "Show Weapons");
            showDoll = GUILayout.Toggle(showDoll, "Show Doll");
            showBuffs = GUILayout.Toggle(showBuffs, "Show Buffs");
            showFx = GUILayout.Toggle(showFx, "Show FX");
            showPortrait = GUILayout.Toggle(showPortrait, "Show Portrait");
            showAsks = GUILayout.Toggle(showAsks, "Show Asks");

            GUILayout.EndHorizontal();
            if (showCharacter) ShowCharacterInfo(unitEntityData);
            if (showWeapons) ShowWeaponInfo(unitEntityData);
            if (showDoll) ShowDollInfo(unitEntityData);
            if (showBuffs) ShowBuffInfo(unitEntityData);
            if (showFx) ShowFxInfo(unitEntityData);
            if (showPortrait) ShowPortraitInfo(unitEntityData);
            if (showAsks) ShowAsksInfo(unitEntityData);

        }
        static void BuildOrphanedEquipment()
        {
            string maleFilepath = "Mods/VisualAdjustments/MaleOrphanedEquipment.json";
            if (File.Exists(maleFilepath))
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader sr = new StreamReader(maleFilepath))
                using (JsonTextReader reader = new JsonTextReader(sr))
                {
                    var result = serializer.Deserialize<UnorderedList<string, string>>(reader);
                    m_OrphanedMaleEquipment = result;
                    if(m_OrphanedMaleEquipment == null) Main.Log($"Error loading {maleFilepath}");
                }
            }
            var femaleFilepath = "Mods/VisualAdjustments/FemaleOrphanedEquipment.json";
            if (File.Exists(femaleFilepath))
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader sr = new StreamReader(femaleFilepath))
                using (JsonTextReader reader = new JsonTextReader(sr))
                {
                    var result = serializer.Deserialize<UnorderedList<string, string>>(reader);
                    m_OrphanedFemaleEquipment = result;
                    if (m_OrphanedFemaleEquipment == null) Main.Log($"Error loading {femaleFilepath}");
                }
            }
            if (m_OrphanedMaleEquipment == null || m_OrphanedFemaleEquipment == null)
            {
                Main.Log("Rebuilding Orphaned Equipment Lookup");
                var eeBlacklist = new HashSet<string>();
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    foreach (var race in BlueprintRoot.Instance.Progression.CharacterRaces)
                    {
                        var armorLinks = ResourcesLibrary.GetBlueprints<KingmakerEquipmentEntity>()
                            .SelectMany(kee => kee.GetLinks(gender, race.RaceId));
                        var options = gender == Gender.Male ? race.MaleOptions : race.FemaleOptions;
                        var links = race.Presets
                            .SelectMany(preset => preset.Skin.GetLinks(gender, race.RaceId))
                            .Concat(armorLinks)
                            .Concat(options.Beards)
                            .Concat(options.Eyebrows)
                            .Concat(options.Hair)
                            .Concat(options.Heads)
                            .Concat(options.Horns);
                        foreach (var link in links)
                        {
                            eeBlacklist.Add(link.AssetId);
                        }
                    }
                }

                m_OrphanedMaleEquipment = new UnorderedList<string, string>();
                m_OrphanedFemaleEquipment = new UnorderedList<string, string>();
                foreach (var kv in ResourcesLibrary.LibraryObject.ResourceNamesByAssetId.OrderBy(kv => kv.Value))
                {
                    if (eeBlacklist.Contains(kv.Key)) continue;
                    var ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(kv.Key);
                    if (ee == null) continue;
                    var nameParts = ee.name.Split('_');
                    bool isMale = nameParts.Contains("M");
                    bool isFemale = nameParts.Contains("F");
                    if (!isMale && !isFemale)
                    {
                        isMale = true;
                        isFemale = true;
                    }
                    if (isMale) m_OrphanedMaleEquipment[kv.Key] = kv.Value;
                    if (isFemale) m_OrphanedFemaleEquipment[kv.Key] = kv.Value;
                }
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                using (StreamWriter sw = new StreamWriter(maleFilepath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, m_OrphanedMaleEquipment);
                }
                using (StreamWriter sw = new StreamWriter(femaleFilepath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, m_OrphanedFemaleEquipment);
                }
                ResourcesLibrary.CleanupLoadedCache();
            }
        }
        static void BuildOrphenedKingmakerEquipment()
        {
            m_OrphanedKingmakerEquipment = new UnorderedList<string, string>();
            var itemLinks = EquipmentResourcesManager.Helm.Keys
                            .Concat(EquipmentResourcesManager.Cloak.Keys)
                            .Concat(EquipmentResourcesManager.Armor.Keys)
                            .Concat(EquipmentResourcesManager.Bracers.Keys)
                            .Concat(EquipmentResourcesManager.Gloves.Keys)
                            .Concat(EquipmentResourcesManager.Boots.Keys)
                            .Distinct()
                            .ToDictionary(key => key);
            foreach (var kee in ResourcesLibrary.GetBlueprints<KingmakerEquipmentEntity>())
            {
                if (!itemLinks.ContainsKey(kee.AssetGuid))
                {
                    m_OrphanedKingmakerEquipment[kee.AssetGuid] = kee.name;
                }
            }
        }
        static string expandedEE = null;
        static void ShowCharacterInfo(UnitEntityData unitEntityData)
        {
            var character = unitEntityData.View.CharacterAvatar;
            if (character == null) return;
            GUILayout.Label($"View: {unitEntityData.View.name}");
            GUILayout.Label($"BakedCharacter: {character.BakedCharacter?.name ?? "NULL"}");

            if (m_OrphanedKingmakerEquipment == null) BuildOrphenedKingmakerEquipment();
            if (m_OrphanedMaleEquipment == null || m_OrphanedFemaleEquipment == null)
            {
                BuildOrphanedEquipment();
            }
            void onEquipment()
            {
                unitEntityData.View.CharacterAvatar.RemoveAllEquipmentEntities();
                var preset = unitEntityData.Descriptor.Progression.Race.Presets.First();
                var skin = preset.Skin.Load(unitEntityData.Gender, preset.RaceId);
                unitEntityData.View.CharacterAvatar.AddEquipmentEntities(skin);
                var kee = ResourcesLibrary.TryGetBlueprint<KingmakerEquipmentEntity>(selectedKingmakerOrphanedEquipment);
                if(kee != null)
                {
                    var ees = kee.Load(unitEntityData.Gender, unitEntityData.Descriptor.Progression.Race.RaceId);
                    unitEntityData.View.CharacterAvatar.AddEquipmentEntities(ees);
                    unitEntityData.View.CharacterAvatar.IsDirty = true;
                }
                var ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(selectedOrphanedEquipment);
                if (ee != null)
                {
                    unitEntityData.View.CharacterAvatar.AddEquipmentEntity(ee);
                    unitEntityData.View.CharacterAvatar.IsDirty = true;
                }
            }
            var equipmentList = unitEntityData.Gender == Gender.Male ? m_OrphanedMaleEquipment : m_OrphanedFemaleEquipment;
            Util.ChooseSlider($"OrphanedKingmakerEquipment", m_OrphanedKingmakerEquipment, ref selectedKingmakerOrphanedEquipment, onEquipment);
            Util.ChooseSlider($"OrphanedEquipment", equipmentList, ref selectedOrphanedEquipment, onEquipment);

            GUILayout.Label("Equipment");
            foreach (var ee in character.EquipmentEntities.ToArray())
            {
                GUILayout.BeginHorizontal();
                if (ee == null)
                {
                    GUILayout.Label("Null");
                } 
                else
                {
                    GUILayout.Label(
                        String.Format("{0}:{1}:{2}:P{3}:S{4}", ee.name, ee.BodyParts.Count, ee.OutfitParts.Count,
                            character.GetPrimaryRampIndex(ee), character.GetSecondaryRampIndex(ee)),
                        GUILayout.ExpandWidth(true));
                }
                if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
                {
                    character.RemoveEquipmentEntity(ee);
                }
                if(ee == null)
                {
                    GUILayout.EndHorizontal();
                    continue;
                }
                bool expanded = ee.name == expandedEE;
                if (expanded && GUILayout.Button("Shrink ", GUILayout.ExpandWidth(false))) expandedEE = null;
                if (!expanded && GUILayout.Button("Expand", GUILayout.ExpandWidth(false))) expandedEE = ee.name;
                GUILayout.EndHorizontal();
                if (expanded)
                {
                    EquipmentEntityInfo settings = lookup.ContainsKey(ee.name) ? lookup[ee.name] : new EquipmentEntityInfo();
                    GUILayout.Label($" HideFlags: {ee.HideBodyParts}");
                    var primaryIndex = character.GetPrimaryRampIndex(ee);
                    Texture2D primaryRamp = null;
                    if (primaryIndex < 0 || primaryIndex > ee.PrimaryRamps.Count - 1) primaryRamp = ee.PrimaryRamps.FirstOrDefault();
                    else primaryRamp = ee.PrimaryRamps[primaryIndex];
                    GUILayout.Label($"PrimaryRamp: {primaryRamp?.name ?? "NULL"}");

                    var secondaryIndex = character.GetSecondaryRampIndex(ee);
                    Texture2D secondaryRamp = null;
                    if (secondaryIndex < 0 || secondaryIndex > ee.SecondaryRamps.Count - 1) secondaryRamp = ee.SecondaryRamps.FirstOrDefault();
                    else secondaryRamp = ee.SecondaryRamps[secondaryIndex];
                    GUILayout.Label($"SecondaryRamp: {secondaryRamp?.name ?? "NULL"}");

                    foreach (var bodypart in ee.BodyParts.ToArray())
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format(" BP {0}:{1}", bodypart?.RendererPrefab?.name ?? "NULL", bodypart?.Type), GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
                        {
                            ee.BodyParts.Remove(bodypart);
                        }
                        GUILayout.EndHorizontal();
                        
                    }
                    foreach (var outfitpart in ee.OutfitParts.ToArray())
                    {
                        GUILayout.BeginHorizontal();
                        var prefab = Traverse.Create(outfitpart).Field("m_Prefab").GetValue<GameObject>();
                        GUILayout.Label(String.Format(" OP {0}:{1}", prefab?.name ?? "NULL", outfitpart?.Special), GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Remove"))
                        {
                            ee.OutfitParts.Remove(outfitpart);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.Label("Character", GUILayout.Width(300));
            GUILayout.Label("RampIndices");
            foreach(var index in Traverse.Create(character).Field("m_RampIndices").GetValue<List<Character.SelectedRampIndices>>())
            {
                var name = index.EquipmentEntity != null ? index.EquipmentEntity.name : "NULL";
                GUILayout.Label($"  {name} - {index.PrimaryIndex}, {index.SecondaryIndex}");
            }
            GUILayout.Label("SavedRampIndices");
            foreach (var index in Traverse.Create(character).Field("m_SavedRampIndices").GetValue<List<Character.SavedSelectedRampIndices>>())
            {
                GUILayout.Label($"  {GetName(index.EquipmentEntityLink)} - {index.PrimaryIndex}, {index.SecondaryIndex}");
            }
            GUILayout.Label("SavedEquipmentEntities");
            foreach (var link in Traverse.Create(character).Field("m_SavedEquipmentEntities").GetValue<List<EquipmentEntityLink>>())
            {
                var name = GetName(link);
                GUILayout.Label($"  {name}");
            }

        }
        static void ShowAsksInfo(UnitEntityData unitEntityData)
        {
            var asks = unitEntityData.Descriptor.Asks;
            var customAsks = unitEntityData.Descriptor.CustomAsks;
            var overrideAsks = unitEntityData.Descriptor.OverrideAsks;
            GUILayout.Label($"Current Asks: {asks?.name}, Display: {asks?.DisplayName}");
            GUILayout.Label($"Current CustomAsks: {customAsks?.name}, Display: {customAsks?.DisplayName}");
            GUILayout.Label($"Current OverrideAsks: {overrideAsks?.name}, Display: {overrideAsks?.DisplayName}");
            foreach (var blueprint in ResourcesLibrary.GetBlueprints<BlueprintUnitAsksList>())
            {
                GUILayout.Label($"Asks: {blueprint}, Display: {blueprint.DisplayName}");
            }

        }
        static void ShowPortraitInfo(UnitEntityData unitEntityData)
        {
            var portrait = unitEntityData.Descriptor.Portrait;
            var portraitBP = unitEntityData.Descriptor.UISettings.PortraitBlueprint;
            var uiPortrait = unitEntityData.Descriptor.UISettings.Portrait;
            var CustomPortrait = unitEntityData.Descriptor.UISettings.CustomPortraitRaw;
            GUILayout.Label($"Portrait Blueprint: {portraitBP}, {portraitBP?.name}");
            GUILayout.Label($"Descriptor Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            GUILayout.Label($"UI Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            GUILayout.Label($"Custom Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            foreach (var blueprint in DollResourcesManager.Portrait.Values)
            {
                GUILayout.Label($"Portrait Blueprint: {blueprint}");
            }
        }
        static void ShowHandslotInfo(HandSlot handSlot)
        {
            GUILayout.BeginHorizontal();
            var pItem = handSlot != null && handSlot.HasItem ? handSlot.Item : null;
            GUILayout.Label(string.Format("Slot {0}, {1}, Active {2}", 
                pItem?.Name, pItem?.GetType(), handSlot?.Active), GUILayout.Width(500));
            if (GUILayout.Button("Remove"))
            {
                handSlot.RemoveItem();
            }
            GUILayout.EndHorizontal();
        }
            static void ShowUnitViewHandSlotData(UnitViewHandSlotData handData)
        {
            var ownerScale = handData.Owner.View.GetSizeScale() * Game.Instance.BlueprintRoot.WeaponModelSizing.GetCoeff(handData.Owner.Descriptor.OriginalSize);
            var visualScale = handData.VisualModel?.transform.localScale ?? Vector3.zero;
            var visualPosition = handData.VisualModel?.transform.localPosition ?? Vector3.zero;
            var sheathScale = handData.SheathVisualModel?.transform.localScale ?? Vector3.zero;
            var sheathPosition = handData.SheathVisualModel?.transform.localPosition ?? Vector3.zero;
            GUILayout.Label(string.Format($"weapon {ownerScale:0.#}, scale {visualScale} position {visualPosition}"), GUILayout.Width(500));
            GUILayout.Label(string.Format($"sheath {ownerScale:0.#}, scale {sheathScale} position {sheathPosition}"), GUILayout.Width(500));
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Data {0} Slot {1} Active {2}", handData?.VisibleItem?.Name, handData?.VisualSlot, handData?.IsActiveSet), GUILayout.Width(500));

            if (GUILayout.Button("Unequip"))
            {
                handData.Unequip();
            }
            if (GUILayout.Button("Swap Slot"))
            {
                handData.VisualSlot += 1;
                if(handData.VisualSlot == UnitEquipmentVisualSlotType.Quiver) handData.VisualSlot = 0;
                handData.Owner.View.HandsEquipment.UpdateAll();
            }
            if (GUILayout.Button("ShowItem 0"))
            {
                handData.ShowItem(false);
            }
            if (GUILayout.Button("ShowItem 1"))
            {
                handData.ShowItem(true);
            }
            GUILayout.EndHorizontal();
        }
        static void ShowWeaponInfo(UnitEntityData unitEntityData)
        {
            GUILayout.Label("Weapons", GUILayout.Width(300));
            var hands = unitEntityData.View.HandsEquipment;
            foreach (var kv in hands.Sets)
            {
                ShowHandslotInfo(kv.Key.PrimaryHand);
                ShowUnitViewHandSlotData(kv.Value.MainHand);
                ShowHandslotInfo(kv.Key.SecondaryHand);
                ShowUnitViewHandSlotData(kv.Value.OffHand);
            }
        }
        static int buffIndex = 0;
        static void ShowBuffInfo(UnitEntityData unitEntityData)
        {
            if (blueprintBuffs.Length == 0)
            {
                BuildLookup();
            }
            GUILayout.BeginHorizontal();
            buffIndex = (int)GUILayout.HorizontalSlider(buffIndex, 0, blueprintBuffs.Length - 1, GUILayout.Width(300));
            if(GUILayout.Button("Prev", GUILayout.Width(45)))
            {
                buffIndex = buffIndex == 0 ? 0 : buffIndex - 1;
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)))
            {
                buffIndex = buffIndex >= blueprintBuffs.Length - 1 ? blueprintBuffs.Length - 1 : buffIndex + 1;
            }
            GUILayout.Label($"{blueprintBuffs[buffIndex].Name}, {blueprintBuffs[buffIndex].name}");
            if (GUILayout.Button("Apply"))
            {
                GameHelper.ApplyBuff(unitEntityData, blueprintBuffs[buffIndex]);
            }
            GUILayout.EndHorizontal();
            foreach(var buff in unitEntityData.Buffs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{buff.Blueprint.name}, {buff.Name}");
                if (GUILayout.Button("Remove"))
                {
                    GameHelper.RemoveBuff(unitEntityData, buff.Blueprint);   
                }
                GUILayout.EndHorizontal();
            }
        }
        static void ShowDollInfo(UnitEntityData unitEntityData)
        {
            var doll = unitEntityData.Descriptor.Doll;
            if(doll == null)
            {
                GUILayout.Label("No Doll");
                return;
            }
            GUILayout.Label("Indices");
            foreach(var kv in doll.EntityRampIdices)
            {
                var ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(kv.Key);
                GUILayout.Label($"{kv.Key} - {ee?.name} - {kv.Value}");
            }
            GUILayout.Label("EquipmentEntities");
            foreach (var id in doll.EquipmentEntityIds)
            {
                var ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(id);
                GUILayout.Label($"{id} - {ee?.name}");
            }
        }
        static string[] FXIds = new string[] { };
        static int fxIndex = 0;
        static void LoadFxLookup(bool forceReload = false)
        {
            var filepath = $"{Main.ModEntry.Path}/fxlookup.txt";
            if (File.Exists(filepath) && !forceReload)
            {
                FXIds = File
                    .ReadAllLines($"{Main.ModEntry.Path}/fxlookup.txt")
                    .Where(id => ResourcesLibrary.LibraryObject.ResourceNamesByAssetId.ContainsKey(id))
                    .ToArray();
            } else { 
                var idList = new List<string>();
                foreach (var kv in ResourcesLibrary.LibraryObject.ResourceNamesByAssetId)
                {
                    var obj = ResourcesLibrary.TryGetResource<UnityEngine.Object>(kv.Key);
                    var go = obj as GameObject;
                    if (go != null && go.GetComponent<PooledFx>() != null)
                    {
                        idList.Add(kv.Key);
                    }
                    ResourcesLibrary.CleanupLoadedCache();
                }
                FXIds = idList
                    .OrderBy(id => ResourcesLibrary.LibraryObject.ResourceNamesByAssetId[id])
                    .ToArray();
                File.WriteAllLines(filepath, FXIds);
            }
        }
        //Refer FxHelper.SpawnFxOnGameObject
        static void ShowFxInfo(UnitEntityData unitEntityData)
        {
            //Choose FX
            GUILayout.Label($"Choose FX {FXIds.Length} available");
            if(FXIds.Length == 0) LoadFxLookup();
            GUILayout.BeginHorizontal();
            fxIndex = (int)GUILayout.HorizontalSlider(fxIndex, 0, FXIds.Length - 1, GUILayout.Width(300));
            if (GUILayout.Button("Prev", GUILayout.Width(45)))
            {
                fxIndex = fxIndex == 0 ? 0 : fxIndex - 1;
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)))
            {
                fxIndex = fxIndex >= FXIds.Length - 1 ? FXIds.Length - 1 : fxIndex + 1;
            }
            var fxId = FXIds[fxIndex];
            GUILayout.Label($"{ResourcesLibrary.LibraryObject.ResourceNamesByAssetId[fxId]} {FXIds[fxIndex]}");
            if (GUILayout.Button("Apply", GUILayout.Width(200)))
            {
                var prefab = ResourcesLibrary.TryGetResource<GameObject>(fxId);
                FxHelper.SpawnFxOnUnit(prefab, unitEntityData.View);
            }
            if (GUILayout.Button("Clear FX Cache", GUILayout.Width(200)))
            {
                LoadFxLookup(forceReload: true);
            }
            GUILayout.EndHorizontal();
            //List of SpawnFxOnStart
            var spawnOnStart = unitEntityData.View.GetComponent<SpawnFxOnStart>();
            if (spawnOnStart)
            {
                GUILayout.Label("Spawn on Start");
                GUILayout.Label("FxOnStart " + spawnOnStart.FxOnStart?.Load()?.name, GUILayout.Width(400));
                GUILayout.Label("FXFxOnDeath " + spawnOnStart.FxOnStart?.Load()?.name, GUILayout.Width(400));
            }
            GUILayout.Label("Decals");
            var decals = Traverse.Create(unitEntityData.View).Field("m_Decals").GetValue<List<FxDecal>>();
            for (int i = decals.Count - 1; i >= 0; i--)
            {
                var decal = decals[i];
                GUILayout.Label("Decal: " + decal.name, GUILayout.Width(400));
                if (GUILayout.Button("Destroy"))
                {
                    GameObject.Destroy(decal.gameObject);
                    decals.RemoveAt(i);
                }
            }
            GUILayout.Label("CustomWeaponEffects");
            var dollroom = Game.Instance.UI.Common.DollRoom;
            foreach(var kv in EffectsManager.WeaponEnchantments)
            {
                GUILayout.Label($"{kv.Key.Name} - {kv.Value.Count}");
                foreach(var go in kv.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  {go?.name ?? "NULL"}");
                    if (dollroom != null && GUILayout.Button("UnscaleFXTimes", GUILayout.ExpandWidth(false)))
                    {
                        Traverse.Create(dollroom).Method("UnscaleFxTimes", new object[] { go }).GetValue();
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Label("FXRoot");
            foreach(Transform t in FxHelper.FxRoot.transform)
            {
                var pooledFX = t.gameObject.GetComponent<PooledFx>();
                var snapToLocaters = (List<SnapToLocator>)AccessTools.Field(typeof(PooledFx), "m_SnapToLocators").GetValue(pooledFX);
                var fxBone = snapToLocaters.Select(s => s.Locator).FirstOrDefault();
                UnitEntityView unit = null;
                if (fxBone != null)
                {
                    var viewTransform = fxBone.Transform;
                    while (viewTransform != null && unit == null)
                    {
                        unit = viewTransform.GetComponent<UnitEntityView>();
                        if (unit == null)
                        {
                            viewTransform = viewTransform.parent;
                        }
                    }
                }
                GUILayout.BeginHorizontal();
                if (unit != null)
                {
                    GUILayout.Label($"{pooledFX.name} - {unit.EntityData.CharacterName} - {unit.name}");
                } else
                {
                    GUILayout.Label($"{pooledFX.name}");
                }
                if(GUILayout.Button("DestroyFX", GUILayout.Width(200))){
                    FxHelper.Destroy(t.gameObject);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}