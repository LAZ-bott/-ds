using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Network;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework;
using System.Reflection;
using static StardewValley.Minigames.MineCart;
using System.Runtime.CompilerServices;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using StardewValley.Extensions;
using StardewValley.Minigames;
using StardewValley.GameData.Characters;
using StardewValley.Objects;
using StardewValley.Menus;
using StardewValley.Triggers;
using StardewValley.Delegates;
using StardewValley.Locations;
using xTile.Dimensions;
using StardewValley.GameData.Crops;
using xTile.Tiles;
using StardewValley.Characters;
using Newtonsoft.Json;
using UnlimitedEventExpansion;
using UnlimitedEventExpansion.Data;
using ContentPatcher;
using StardewValley.GameData.Objects;
using StardewValley.Pathfinding;


namespace UnlimitedEventExpansion
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry
    {
        //
        // *************************** ENTRY ***************************
        //


        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            ModEntry.Instance = this;

            SMonitor = Monitor;
            SHelper = helper;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.OneSecondUpdateTicked += onOneSecondUpdated;


            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.PlayerWarpper", PlayerWarpper);
            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.setupCamp", setupCamp);
            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.endCamp", endCamp);
            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.campfireSkipWarpper", campfireSkipWarpper);
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool PlayerWarpper(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[2], out int x) || !int.TryParse(args[3], out int y))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }


            DelayedAction.functionAfterDelay(() =>
            {
                Game1.warpFarmer(locationName, x, y, false);
            }, 1500);

            totalSkippedEvent += 1;
            if (totalSkippedEvent >= 3)
            {
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^You have skipped too many events. You will no longer able to get new event during this session.^^It is sad!!!^HaPyke.");
                iAppMessengerApi.SendSmartphoneMessageFromNPC(args[4], "I am very unhappy that you left at the middle of our conversation. It is very rude of you!");
            }
            else if (totalSkippedEvent >= 1)
            {
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^Please note that each event cost $real-world USD$ to create. If you don’t wish to participate in an event, PLEASE <<cancel the invitation.<<^^" +
                    "]] Continuing to skip events may temporary prevent you from getting new events.^^Thank you for your understanding, HaPyke +++");
                iAppMessengerApi.SendSmartphoneMessageFromNPC(args[4], "You dissappeared at the middle of our conversation. Please let me know next time so I don't have to look for you around.");
            }

            return true;
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool setupCamp(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int y))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }




            var campfire = new Torch("146", true);
            campfire.IsOn = true;
            if (!Game1.currentLocation.objects.ContainsKey(new Vector2(x, y)))
            {
                Game1.currentLocation.objects.Add(new Vector2(x, y), campfire);
                campfire.initializeLightSource(new Vector2(x, y));
            }
            return true;
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool endCamp(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int y))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }




            if (Game1.currentLocation.objects.TryGetValue(new Vector2(x, y), out StardewValley.Object obj))
            {
                if (obj is Torch torch && obj.ParentSheetIndex == 146)
                {
                    torch.performRemoveAction();
                    Game1.currentLocation.objects.Remove(new Vector2(x, y));

                }
            }

            return true;
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool campfireSkipWarpper(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[2], out int x) || !int.TryParse(args[3], out int y) || !int.TryParse(args[4], out int x1) || !int.TryParse(args[5], out int y1))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }

            if (Game1.currentLocation.objects.TryGetValue(new Vector2(x1, y1), out StardewValley.Object obj))
            {
                if (obj is Torch torch && obj.ParentSheetIndex == 146)
                {
                    // Properly clean up
                    torch.performRemoveAction();
                    Game1.currentLocation.objects.Remove(new Vector2(x1, y1));

                }
            }

            DelayedAction.functionAfterDelay(() =>
            {
                Game1.warpFarmer(locationName, x, y, false);
            }, 1500);


            totalSkippedEvent += 1;
            if (totalSkippedEvent >= 3)
            {
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^You have skipped too many events. You will no longer able to get new event during this session.^^It is sad!!!^HaPyke.");
                iAppMessengerApi.SendSmartphoneMessageFromNPC(args[4], "I am very unhappy that you left at the middle of our conversation. It is very rude of you!");
            }
            else if (totalSkippedEvent >= 1)
            {
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^Please note that each event cost $real-world USD$ to create. If you don’t wish to participate in an event, PLEASE <<cancel the invitation.<<^^" +
                    "]] Continuing to skip events may temporary prevent you from getting new events.^^Thank you for your understanding, HaPyke +++");
                iAppMessengerApi.SendSmartphoneMessageFromNPC(args[4], "You dissappeared at the middle of our conversation. Please let me know next time so I don't have to look for you around.");
            }

            return true;
        }

        //
        // ***************************  END OF ENTRY ***************************
        //

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            this.Monitor.Log("Loading Unlimited Event Expansion.", LogLevel.Trace);
            var api = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            ConfigMenu(api, this.ModManifest, Helper);

            iAppMessengerApi = SHelper.ModRegistry.GetApi<IAppMessengerApi>("d5a1lamdtd.Smartphone-AppMessenger");
            iSmartphoneApi = SHelper.ModRegistry.GetApi<ISmartphoneApi>("d5a1lamdtd.Smartphone");

            if (iAppMessengerApi == null)
            {
                Monitor.Log("Smartphone mod is not installed.", LogLevel.Warn);
            }
            else
            {
                this.Monitor.Log("Smartphone loaded.", LogLevel.Trace);
                bool AllowEarlyEvent = !string.IsNullOrWhiteSpace(Config.Key) && Config.AllowEarlyEvent;

                iAppMessengerApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Birthday",
                    triggerEvent: npcName => TriggerNpcBirthdayEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 2,
                    toolDescription: "");

                iAppMessengerApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Campfire",
                    triggerEvent: npcName => TriggerCampingEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 3,
                    toolDescription: "");

                iAppMessengerApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Picnic",
                    triggerEvent: npcName => TriggerPicnicEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 4,
                    toolDescription: "");

                iAppMessengerApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Dine Out",
                    triggerEvent: npcName => TriggerDineOutEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 5,
                    toolDescription: "");
            }
        }


        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            string targetModId = this.ModManifest.UniqueID;
            var modInfo = this.Helper.ModRegistry.Get(targetModId);

            if (modInfo != null)
            {
                Task.Run(async () =>
                {
                    bool hasNewerVersion = await CheckForNewerVersion(modInfo);

                    if (hasNewerVersion)
                    {
                        DelayedAction.functionAfterDelay(() =>
                        {
                            try
                            {
                                SMonitor.Log($"UnlimitedEventExpansion: Newer version available", LogLevel.Warn);

                                iSmartphoneApi.SendSmartphoneNotification(GetTranslation("notification.new-version"), GetTranslation("app.name"));
                            }
                            catch (Exception ex)
                            {
                                SMonitor.Log($"UnlimitedEventExpansion: Unable to notify about newer version: {ex}", LogLevel.Trace);
                            }
                        }, 10000);
                    }
                });
            }

            string theme = Config.NpcProfileTheme;
            if (string.IsNullOrWhiteSpace(theme) || !System.IO.Directory.Exists(System.IO.Path.Combine(this.Helper.DirectoryPath, "npc_profile", theme)))
            {
                theme = "vanilla";
            }

            string npc_characteristic = Helper.ModContent.GetInternalAssetName($"npc_profile/{theme}/npc_characteristics_short.json").BaseName;
            string npc_characteristic_minimal = Helper.ModContent.GetInternalAssetName($"npc_profile/{theme}/npc_characteristics_minimal.json").BaseName;
            string npc_characteristic_long = Helper.ModContent.GetInternalAssetName($"npc_profile/{theme}/npc_characteristics_long.json").BaseName;

            NpcCharacteristicsShort = Helper.ModContent.Load<Dictionary<string, string>>(npc_characteristic);
            NpcCharacteristicsMinimal = Helper.ModContent.Load<Dictionary<string, string>>(npc_characteristic_minimal);
            NpcCharacteristicsLong = Helper.ModContent.Load<Dictionary<string, string>>(npc_characteristic_long);

            string npc_portraits = Helper.ModContent.GetInternalAssetName("assets/npc_portraits.json").BaseName;
            NpcPortrait = Helper.ModContent.Load<Dictionary<string, string>>(npc_portraits);

            string birthdayMapAsset = Helper.ModContent.GetInternalAssetName("assets/map_birthday.json").BaseName;
            birthdayMap = Helper.ModContent.Load<Dictionary<string, BirthdayMapData>>(birthdayMapAsset);

            string picnicMapAsset = Helper.ModContent.GetInternalAssetName("assets/map_picnic.json").BaseName;
            picnicMap = Helper.ModContent.Load<Dictionary<string, PicnicMapData>>(picnicMapAsset);

            string campfireMapAsset = Helper.ModContent.GetInternalAssetName("assets/map_campfire.json").BaseName;
            campfireMap = Helper.ModContent.Load<Dictionary<string, CampfireMapData>>(campfireMapAsset);

            HashSet<string> universalHates = new HashSet<string>();
            HashSet<string> universalDislikes = new HashSet<string>();

            foreach (var entry in Game1.NPCGiftTastes)
            {
                if (entry.Key == "Universal_Hate")
                {
                    foreach (string id in entry.Value.Split(' '))
                        universalHates.Add(id);
                }
                else if (entry.Key == "Universal_Dislike")
                {
                    foreach (string id in entry.Value.Split(' '))
                        universalDislikes.Add(id);
                }
            }

            foreach (var pair in Game1.objectData)
            {
                var baseData = pair.Value;
                string baseId = pair.Key;


                if (baseData.Category == -7 && baseData.Price >= 150 && baseData.Price <= 1000
                    && !baseData.Name.Contains("Pickled") && !baseData.Name.Contains("Elixir") && !baseData.Name.Contains("Roe") && !baseData.Name.Contains("Mayonnaise") && !baseData.Name.Contains("Smoked")
                    && !baseData.Name.Contains("Oil") && !baseData.Name.Contains("Jelly") && !baseData.Name.Contains("Honey") && !baseData.Name.Contains("Wine") && !baseData.Name.Contains("Dried")
                    && !baseData.Name.Contains("Juice") && !universalHates.Contains(baseId) && !universalDislikes.Contains(baseId))
                {
                    var item = new StardewValley.Object(baseId, 1);
                    cookingItems.Add(item);
                }

            }
        }

        private void onOneSecondUpdated(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null || Game1.currentLocation == null)
                return;

            if (Game1.eventUp || Game1.activeClickableMenu != null || !Context.IsPlayerFree)
                return;

            if (!TryPeekGeneratedEvent(out QueuedEventStart queuedEvent))
                return;

            GameLocation location = Game1.getLocationFromName(queuedEvent.LocationName);
            if (location == null)
            {
                TryDequeueGeneratedEvent(queuedEvent);
                return;
            }
            Game1.activeClickableMenu = null;
            if (Game1.player.currentLocation != location)
            {
                Game1.warpFarmer(queuedEvent.LocationName, queuedEvent.WarpX, queuedEvent.WarpY, queuedEvent.FacingDirection);

                // farmhand will have to wait
                if (!Game1.IsMasterGame)
                    return;
            }

            if (!TryDequeueGeneratedEvent(queuedEvent))
                return;

            location.startEvent(queuedEvent.Event);

            Game1.player.completelyStopAnimatingOrDoingAction();
            Game1.player.Halt();
            Game1.player.canMove = false;
            Game1.player.freezePause = 0;
        }

        public static bool onItemChange(Item i, int position, Item old, StorageContainer container, bool onRemoval)
        {
            if (!onRemoval)
            {
                if (i.Stack > 1 || i.Stack == 1 && old is { Stack: 1 } && i.canStackWith(old))
                {
                    if (old != null && old.canStackWith(i))
                    {
                        container.ItemsToGrabMenu.actualInventory[position].Stack = 1;
                        container.heldItem = old;
                        return false;
                    }

                    if (old != null)
                    {
                        StardewValley.Utility.addItemToInventory(old, position,
                            container.ItemsToGrabMenu.actualInventory);
                        container.heldItem = i;
                        return false;
                    }


                    int allButOne = i.Stack - 1;
                    Item reject = i.getOne();
                    reject.Stack = allButOne;
                    container.heldItem = reject;
                    i.Stack = 1;
                }
            }
            else if (old is { Stack: > 1 })
            {
                if (!old.Equals(i))
                {
                    return false;
                }
            }

            var itemToAdd = onRemoval && (old == null || old.Equals(i)) ? null : i;

            string itemNames = string.Join(", ", container.ItemsToGrabMenu.actualInventory
                .Where(item => item != null) // Filter out null items
                .Select(item => item.DisplayName)); // Get the display name for each item

            birthdayGiftName = itemNames;

            return true;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CheckAIUsage();
            ClearGeneratedEventQueue();
            totalSkippedEvent = 0;
            PendingUnlimitedEvents.Clear();
            TotalEventRegisteredToday = 0;

            CheckTodayPlayerBirthday();
        }
        public static string ConvertPathToAdvancedMove(Point startPoint, Stack<Point> path)
        {
            if (path == null || path.Count == 0) return "";

            var moveString = new System.Text.StringBuilder();

            Point currentPos = startPoint;
            int accumX = 0;
            int accumY = 0;

            foreach (Point nextPos in path)
            {
                if (nextPos == currentPos) continue;

                int dx = nextPos.X - currentPos.X;
                int dy = nextPos.Y - currentPos.Y;

                if ((dx != 0 && accumY != 0) || (dy != 0 && accumX != 0))
                {
                    if (accumX != 0 || accumY != 0)
                    {
                        moveString.Append($"{accumX} {accumY} ");
                    }
                    accumX = 0;
                    accumY = 0;
                }

                accumX += dx;
                accumY += dy;

                currentPos = nextPos;
            }

            if (accumX != 0 || accumY != 0)
            {
                moveString.Append($"{accumX} {accumY}");
            }

            return moveString.ToString().Trim();
        }


        public static (string, string, string) RandomPathCommand(string locationName, int finalX, int finalY, int finalFacingDirection, string npcName, string hiTarget)
        {
            var endPoint = new Point(finalX, finalY);
            var location = Game1.getLocationFromName(locationName);
            var character = Game1.getCharacterFromName(npcName);

            if (character == null)
            {
                character = Game1.getCharacterFromName("Lewis");
            }

            PathFindController.isAtEnd endFunction = delegate (PathNode currentNode, Point end, GameLocation loc, Character c)
            {
                return currentNode.x == end.X && currentNode.y == end.Y;
            };

            Point startPoint = Point.Zero;
            Stack<Point> absolutePath = null;
            bool pathFound = false;

            for (int i = 0; i < 10; i++)
            {
                int offsetX = Game1.random.Next(-9, 12);
                int offsetY = Game1.random.Next(-7, 8);

                if (Math.Abs(offsetX) < 5 && Math.Abs(offsetY) < 5) continue;

                Point testStartPoint = new Point(endPoint.X + offsetX, endPoint.Y + offsetY);

                if (testStartPoint == endPoint || !IsWalkableWarpTile(location, testStartPoint.X, testStartPoint.Y)) continue;

                absolutePath = PathFindController.findPath(
                    testStartPoint, endPoint, endFunction, location, character, 30
                );

                if (absolutePath != null)
                {
                    startPoint = testStartPoint;
                    pathFound = true;
                    break;
                }
            }

            if (!pathFound)
            {
                return ($"{finalX} {finalY} {finalFacingDirection}", "", "");
            }

            string dynamicPath = ConvertPathToAdvancedMove(startPoint, absolutePath);

            string startPosition = "";
            string moveCommand = "";
            string proceedCommand = "";

            if (!string.IsNullOrEmpty(dynamicPath))
            {
                var startNpcPosition = $"{startPoint.X} {startPoint.Y}";
                var finalNpcPosition = $"{endPoint.X} {endPoint.Y}";

                // Piece 1: Just the coordinates and direction
                startPosition = $"{startNpcPosition} {finalFacingDirection}";

                // Piece 2: The actual movement and text
                moveCommand = $"/advancedMove {npcName} false {dynamicPath} {(finalFacingDirection == 0 ? 4 : finalFacingDirection)} 1000";

                if (Game1.random.NextDouble() < 0.5)
                {
                    moveCommand += $"/pause {Game1.random.Next(200, 501)}/textAboveHead {npcName} \"{Game1.random.Choose("Hey", "Hi", "Hello", "How are you")} {hiTarget}\"";
                }

                // Piece 3: The blocker
                proceedCommand = $"/proceedPosition {npcName} {finalNpcPosition}";
            }
            else
            {
                // Fallback
                return ($"{finalX} {finalY} {finalFacingDirection}", "", "");
            }

            return (startPosition, moveCommand, proceedCommand);
        }

        private static bool IsWalkableWarpTile(GameLocation location, int tileX, int tileY)
        {
            var tile = new Vector2(tileX, tileY);
            if (!location.CanSpawnCharacterHere(tile))
                return false;

            return true;
        }


        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (PendingUnlimitedEvents.Count > 0 && Context.IsPlayerFree && e.NewTime < 2500)
            {
                foreach (var scheduledEvent in PendingUnlimitedEvents.ToList())
                {
                    if (!int.TryParse(scheduledEvent.TimeOfDay, out int eventTime))
                    {
                        PendingUnlimitedEvents.Remove(scheduledEvent);
                        continue;
                    }

                    if (e.NewTime >= eventTime)
                    {
                        string eventType = scheduledEvent.EventType;
                        string npcName = scheduledEvent.NpcName;

                        switch (eventType.ToLower())
                        {
                            case "birthday":
                                TriggerNpcBirthdayEvent(npcName, scheduledEvent.LocationName, scheduledEvent.ParticipantNames);
                                break;
                            case "campfire":
                                TriggerCampingEvent(npcName, scheduledEvent.LocationName, scheduledEvent.ParticipantNames);
                                break;
                            case "picnic":
                                TriggerPicnicEvent(npcName, scheduledEvent.LocationName, scheduledEvent.ParticipantNames);
                                break;
                            case "dine out":
                                TriggerDineOutEvent(npcName);
                                break;
                            case "playerbirthday":
                                TriggerPlayerBirthdayEvent(scheduledEvent.LocationName, scheduledEvent.ParticipantNames);
                                break;
                        }

                        PendingUnlimitedEvents.Remove(scheduledEvent);
                        break;
                    }
                }
            }

            return;
        }

        private static async Task<(bool IsLatest, string? LatestVersion, string? LatestUrl)> CheckForModUpdate(IModInfo modInfo)
        {
            if (modInfo?.Manifest == null)
                return (true, null, null);

            var request = new
            {
                mods = new[]
                {
                    new
                    {
                        id = modInfo.Manifest.UniqueID,
                        updateKeys = modInfo.Manifest.UpdateKeys,
                        installedVersion = modInfo.Manifest.Version.ToString(),
                        isBroken = false
                    }
                },
                apiVersion = Constants.ApiVersion.ToString(),
                gameVersion = Game1.version.ToString(),
                platform = Constants.TargetPlatform.ToString(),
                includeExtendedMetadata = false
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            using var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request, jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            using HttpResponseMessage response = await new HttpClient().PostAsync($"https://smapi.io/api/v{Constants.ApiVersion}/mods", content);
            response.EnsureSuccessStatusCode();

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement result = document.RootElement[0];

            if (result.TryGetProperty("suggestedUpdate", out JsonElement suggestedUpdate) && suggestedUpdate.ValueKind == JsonValueKind.Object)
            {
                return (
                    false,
                    suggestedUpdate.GetProperty("version").GetString(),
                    suggestedUpdate.GetProperty("url").GetString()
                );
            }

            return (true, null, null);
        }

        public static async Task<bool> CheckForNewerVersion(IModInfo? modInfo)
        {
            if (modInfo?.Manifest == null)
                return false;

            var update = await CheckForModUpdate(modInfo);
            return !update.IsLatest;
        }


        public static bool CanTriggerEvent()
        {
            return totalSkippedEvent < 3;
        }

        private static object ResolveReasoningForModel(string? model)
        {
            return model switch
            {
                ModConfig.ModelGpt51 => new { effort = "none" },
                ModConfig.ModelGpt54Mini => new { effort = "none" },
                ModConfig.ModelGpt54Nano => new { effort = "none" },
                ModConfig.ModelGpt5Mini => new { effort = "minimal" },
                ModConfig.ModelGpt5Nano => new { effort = "minimal" },
                ModConfig.ModelGemini35Flash => new { thinkingLevel = "MINIMAL" },
                ModConfig.ModelGemini31FlashLite => new { thinkingLevel = "MINIMAL" },
                ModConfig.ModelGemini3FlashPreview => new { thinkingLevel = "MINIMAL" },
                _ => new { effort = "minimal" }
            };
        }

        internal static void CheckAIUsage()
        {
            if (string.Equals(Config.ApiProvider, ModConfig.ProviderCustom, StringComparison.OrdinalIgnoreCase))
            {
                EventModel = Config.Model?.Trim() ?? string.Empty;
                EventKey = Config.Key?.Trim() ?? string.Empty;
                EventReasoning = ResolveReasoningForModel(EventModel);
                IsMaxedLimit = string.IsNullOrWhiteSpace(EventKey) || string.IsNullOrWhiteSpace(EventModel);
                if (IsMaxedLimit)
                    SMonitor.Log("Custom API is selected, but the API key or model is empty. Configure both in Generic Mod Config Menu.", LogLevel.Warn);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Config.Key))
            {
                EventModel = Config.Model;
                EventKey = Config.Key;
                EventReasoning = ResolveReasoningForModel(EventModel);
                IsMaxedLimit = false;
                return;
            }

            string primaryRuntimeKey = EmbeddedAiSecrets.SharedOpenAiRuntimeKeyPrimary;
            string primaryAdminKey = EmbeddedAiSecrets.SharedOpenAiAdminKeyPrimary;
            string secondaryRuntimeKey = EmbeddedAiSecrets.SharedOpenAiRuntimeKeySecondary;
            string secondaryAdminKey = EmbeddedAiSecrets.SharedOpenAiAdminKeySecondary;

            bool hasPrimaryPool = !string.IsNullOrWhiteSpace(primaryRuntimeKey) && !string.IsNullOrWhiteSpace(primaryAdminKey);
            bool hasSecondaryPool = !string.IsNullOrWhiteSpace(secondaryRuntimeKey) && !string.IsNullOrWhiteSpace(secondaryAdminKey);

            if (!hasPrimaryPool && !hasSecondaryPool)
            {
                EventKey = "";
                IsMaxedLimit = true;
                SMonitor.Log("Shared OpenAI fallback keys are missing. Add them to .env for this build.", LogLevel.Warn);
                return;
            }

            Task.Run(async () =>
            {
                if (hasPrimaryPool)
                {
                    var (primaryPremium, _) = await GetOpenAIUsage(primaryAdminKey);
                    if (primaryPremium != -1 && primaryPremium < 1000000)
                    {
                        switched = false;
                        IsMaxedLimit = false;
                        EventKey = primaryRuntimeKey;
                        EventModel = ModConfig.ModelGpt51;
                        EventReasoning = ResolveReasoningForModel(EventModel);
                        return;
                    }

                    switched = true;
                }
                else
                {
                    switched = true;
                }

                if (!hasSecondaryPool)
                {
                    EventKey = "";
                    IsMaxedLimit = true;
                    SMonitor.Log("Secondary shared OpenAI fallback keys are missing. Shared AI usage is disabled.", LogLevel.Warn);
                    return;
                }

                var (premium, regular) = await GetOpenAIUsage(secondaryAdminKey);
                if (premium == -1 || regular == -1)
                {
                    totalFailedCheck += 1;
                    if (totalFailedCheck >= 3)
                    {
                        EventKey = "";
                        IsMaxedLimit = true;
                        iSmartphoneApi.SendSmartphoneNotification(GetTranslation("notification.check-usage-failed"), GetTranslation("app.name"));
                        return;
                    }
                }

                IsMaxedLimit = false;
                if (regular > 4500000 && premium > 250000)
                {
                    EventKey = "";
                    IsMaxedLimit = true;
                    iSmartphoneApi.SendSmartphoneNotification(GetTranslation("notification.usage-limit-reached"), GetTranslation("app.name"));
                    return;
                }

                EventKey = secondaryRuntimeKey;
                if (premium > 250000)
                {
                    EventModel = ModConfig.ModelGpt54Mini;
                    EventReasoning = ResolveReasoningForModel(EventModel);
                }
                else
                {
                    EventModel = ModConfig.ModelGpt51;
                    EventReasoning = ResolveReasoningForModel(EventModel);
                }

                if (regular > 3000000)
                {
                    EventModel = ModConfig.ModelGpt5Nano;
                    EventReasoning = ResolveReasoningForModel(EventModel);
                }
                else if (regular > 2500000)
                {
                    EventModel = ModConfig.ModelGpt5Mini;
                    EventReasoning = ResolveReasoningForModel(EventModel);
                }
                else if (premium > 250000 && regular < 2500000)
                {
                    EventModel = ModConfig.ModelGpt54Mini;
                    EventReasoning = ResolveReasoningForModel(EventModel);
                }
            });
        }
    }
}