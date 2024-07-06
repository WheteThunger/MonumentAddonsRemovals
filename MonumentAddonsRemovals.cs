using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

using InitializeAddonCallback = System.Func<BasePlayer, UnityEngine.Vector3, object>;
using SpawnAddonCallback = System.Func<UnityEngine.Vector3, UnityEngine.Quaternion, Newtonsoft.Json.Linq.JObject, UnityEngine.Component>;
using KillAddonCallback = System.Action<UnityEngine.Component>;
using UpdateAddonCallback = System.Action<UnityEngine.Component, Newtonsoft.Json.Linq.JObject>;
using AddDisplayInfoCallback = System.Action<UnityEngine.Component, Newtonsoft.Json.Linq.JObject, System.Text.StringBuilder>;
using SetAddonDataCallback = System.Action<UnityEngine.Component, object>;

namespace Oxide.Plugins
{
    [Info("Monument Addons Removals", "WhiteThunder", "0.1.0")]
    [Description("Addon plugin for Monument Addons that allows removing entities from monuments.")]
    internal class MonumentAddonsRemovals : CovalencePlugin
    {
        #region Fields

        [PluginReference]
        private readonly Plugin MonumentAddons;

        private const string MonumentAddonsAdminPermission = "monumentaddons.admin";

        private SetAddonDataCallback _setAddonData;

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            if (MonumentAddons == null)
            {
                LogError($"{nameof(MonumentAddons)} is not loaded, get it at https://umod.org");
                return;
            }

            RegisterCustomAddon();
        }

        private void OnPluginLoaded(Plugin plugin)
        {
            if (plugin == MonumentAddons)
            {
                RegisterCustomAddon();
            }
        }

        #endregion

        #region Commands

        [Command("mar.add", "mar.remove", "mar.radius")]
        private void CommandUpdate(IPlayer player, string cmd, string[] args)
        {
            if (!VerifyPlayer(player, out var basePlayer)
                || !VerifyHasPermission(player, MonumentAddonsAdminPermission))
                return;

            if (!TryGetHitPosition(basePlayer, out var position))
            {
                // TODO: Lang
                player.Reply($"No hit");
                return;
            }

            var maxDistanceSquared = 9;
            var removerComponent = RemoverComponent.InstanceList
                .Select(component => (component, (component.transform.position - position).sqrMagnitude))
                .OrderBy(tuple => tuple.Item2)
                .First(tuple => tuple.Item2 < maxDistanceSquared).component;

            if (removerComponent == null)
            {
                // TODO: Lang
                player.Reply($"No remover found");
                return;
            }

            var removerData = removerComponent.Data ?? new RemoverData();

            if (cmd == "mar.add")
            {
                if (args.Length < 0)
                {
                    // TODO: Lang
                    player.Reply($"Syntax: {cmd} <prefab>");
                    return;
                }

                var prefabName = args[0].ToLower();
                if (!removerData.PrefabNames.Contains(prefabName))
                {
                    removerData.PrefabNames.Add(prefabName);
                }

                _setAddonData.Invoke(removerComponent, removerData);

                // TODO: Lang
                player.Reply($"Added prefab to remover: {prefabName}");
                return;
            }

            if (cmd == "mar.remove")
            {
                if (args.Length < 0)
                {
                    // TODO: Lang
                    player.Reply($"Syntax: {cmd} <prefab>");
                    return;
                }

                var prefabName = args[0].ToLower();
                removerData.PrefabNames.Remove(prefabName);

                _setAddonData.Invoke(removerComponent, removerData);

                // TODO: Lang
                player.Reply($"Removed prefab from remover: {prefabName}");
                return;
            }

            if (cmd == "mar.radius")
            {
                if (args.Length < 0 || !float.TryParse(args[0], out var radius))
                {
                    // TODO: Lang
                    player.Reply($"Syntax: {cmd} <number>");
                    return;
                }

                removerData.Radius = radius;
                _setAddonData.Invoke(removerComponent, removerData);

                // TODO: Lang
                player.Reply($"Updated radius for remover: {radius}");
                return;
            }
        }

        #endregion

        #region Helpers

        private static bool TryRaycast(BasePlayer player, out RaycastHit hit, float maxDistance = 100)
        {
            return Physics.Raycast(player.eyes.HeadRay(), out hit, maxDistance, Rust.Layers.Solid, QueryTriggerInteraction.Ignore);
        }

        private static bool TryGetHitPosition(BasePlayer player, out Vector3 position)
        {
            if (TryRaycast(player, out var hit))
            {
                position = hit.point;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        private bool VerifyPlayer(IPlayer player, out BasePlayer basePlayer)
        {
            if (player.IsServer)
            {
                basePlayer = null;
                return false;
            }

            basePlayer = player.Object as BasePlayer;
            return true;
        }

        private bool VerifyHasPermission(IPlayer player, string perm)
        {
            if (player.HasPermission(perm))
                return true;

            ReplyToPlayer(player, LangEntry.ErrorNoPermission);
            return false;
        }

        private void ReplyToPlayer(IPlayer player, LangEntry langEntry)
        {
            player.Reply(GetMessage(player.Id, langEntry));
        }

        private bool IsMonumentAddonsEntity(BaseEntity entity)
        {
            return MonumentAddons.Call("API_IsMonumentEntity", entity) is true;
        }

        private Component SpawnAddon(Vector3 position, Quaternion rotation, JObject data)
        {
            return RemoverComponent.Create(this, position, data?.ToObject<RemoverData>());
        }

        private object InitializeAddon(BasePlayer player, Vector3 position)
        {
            // return new RemoverData();
            return null;
        }

        private void UpdateAddon(Component component, JObject data)
        {
            if (component is not RemoverComponent removerComponent)
                return;

            removerComponent.UpdateData(data?.ToObject<RemoverData>());
        }

        private void KillAddon(Component component)
        {
            if (component is not RemoverComponent)
                return;

            UnityEngine.Object.Destroy(component);
        }

        private void AddDisplayInfo(Component component, JObject data, StringBuilder sb)
        {
            if (component is not RemoverComponent)
                return;

            var removerData = data?.ToObject<RemoverData>();
            if (removerData == null)
                return;

            sb.AppendLine($"Radius: {removerData.Radius}");
            sb.AppendLine($"Prefabs to remove:\n{string.Join("\n", removerData.PrefabNames)}");
        }

        private void RegisterCustomAddon()
        {
            var addonHandler = MonumentAddons.Call("API_RegisterCustomAddon", this, "remover",
                new Dictionary<string, object>
                {
                    ["Initialize"] = new InitializeAddonCallback(InitializeAddon),
                    ["Spawn"] = new SpawnAddonCallback(SpawnAddon),
                    ["Kill"] = new KillAddonCallback(KillAddon),
                    ["Update"] = new UpdateAddonCallback(UpdateAddon),
                    ["AddDisplayInfo"] = new AddDisplayInfoCallback(AddDisplayInfo),
                }
            ) as Dictionary<string, object>;

            if (addonHandler == null)
            {
                LogError($"Error registering addon with Monument Addons.");
                return;
            }

            _setAddonData = addonHandler["SetData"] as SetAddonDataCallback;
            if (_setAddonData == null)
            {
                LogError($"SetData method not returned");
                return;
            }
        }

        #endregion

        #region Component

        private class RemoverComponent : ListComponent<RemoverComponent>
        {
            public static RemoverComponent Create(MonumentAddonsRemovals plugin, Vector3 position, RemoverData data)
            {
                var gameObject = new GameObject();
                gameObject.transform.position = position;

                var component = gameObject.AddComponent<RemoverComponent>();
                component._plugin = plugin;
                component.UpdateData(data);

                return component;
            }

            private MonumentAddonsRemovals _plugin;
            public RemoverData Data { get; private set; }

            public void UpdateData(RemoverData data)
            {
                Data = data;
                RemoveMatchingEntities();
            }

            private void RemoveMatchingEntities()
            {
                if (Data == null)
                    return;

                var list = Facepunch.Pool.GetList<BaseEntity>();
                Vis.Entities(transform.position, Data.Radius, list, Rust.Layers.Solid, QueryTriggerInteraction.Ignore);

                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var entity = list[i];
                    if (!Data.PrefabNames.Contains(entity.PrefabName, StringComparer.InvariantCultureIgnoreCase))
                        continue;

                    if (entity == null || entity.IsDestroyed)
                        continue;

                    if (_plugin.IsMonumentAddonsEntity(entity))
                        continue;

                    entity.Kill();
                }

                Facepunch.Pool.FreeList(ref list);
            }
        }

        #endregion

        #region Data

        private class RemoverData
        {
            [JsonProperty("Radius")]
            public float Radius = 3f;

            [JsonProperty("PrefabNames")]
            public List<string> PrefabNames = new();
        }

        #endregion

        #region Localization

        private class LangEntry
        {
            public static List<LangEntry> AllLangEntries = new();

            public static readonly LangEntry ErrorNoPermission = new("Error.NoPermission", "You don't have permission to do that.");

            public string Name;
            public string English;

            public LangEntry(string name, string english)
            {
                Name = name;
                English = english;

                AllLangEntries.Add(this);
            }
        }

        private string GetMessage(string playerId, LangEntry langEntry)
        {
            return lang.GetMessage(langEntry.Name, this, playerId);
        }

        protected override void LoadDefaultMessages()
        {
            var englishLangKeys = new Dictionary<string, string>();

            foreach (var langEntry in LangEntry.AllLangEntries)
            {
                englishLangKeys[langEntry.Name] = langEntry.English;
            }

            lang.RegisterMessages(englishLangKeys, this);
        }

        #endregion
    }
}
