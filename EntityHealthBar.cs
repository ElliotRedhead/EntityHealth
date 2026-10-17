namespace Oxide.Plugins
{
	using System.Collections.Generic;
	using System.Globalization;
	using Oxide.Core.Plugins;
	using Oxide.Game.Rust.Cui;
	using Rust.Ai.Gen2;
	using UnityEngine;

	[Info("EntityHealthBar", "ExpertRaptor", "0.1.1")]
	[Description("Shows an on-screen health bar for the last enemy a player damaged.")]
	class EntityHealthBar : RustPlugin
	{
		const string BarPanelName = "EntityHealthBar.Bar";
		const float RefreshIntervalSeconds = 0.5f;
		const float MaxTrackingDistance = 100f;

		// Rust entities have no display name, so known prefabs get one here; others fall back to a tidied prefab name.
		static readonly Dictionary<string, string> NamesByShortPrefabName = new Dictionary<string, string>
		{
			{ "wolf2", "Wolf" },
			{ "bear", "Bear" },
			{ "polarbear", "Polar Bear" },
			{ "boar", "Boar" },
			{ "stag", "Deer" },
			{ "chicken", "Chicken" },
			{ "tiger", "Tiger" },
			{ "panther", "Panther" },
			{ "crocodile", "Crocodile" },
			{ "snake.entity", "Snake" },
			{ "simpleshark", "Shark" },
			{ "rabbit", "Rabbit" },
			{ "cow", "Cow" },
			{ "bull", "Bull" },
			{ "sheep", "Sheep" },
			{ "lamb", "Lamb" },
			{ "zombie", "Zombie" },
			{ "scientistnpc_heavy", "Heavy Scientist" },
			{ "scientistnpc_bradley_heavy", "Heavy Scientist" },
			{ "scientist2.heavy", "Heavy Scientist" },
			{ "npc_tunneldweller", "Tunnel Dweller" },
			{ "npc_underwaterdweller", "Underwater Dweller" },
			{ "npc_bandit_guard", "Bandit Guard" },
			{ "scarecrow", "Scarecrow" },
			{ "scarecrow_dungeon", "Scarecrow" },
			{ "gingerbread_dungeon", "Gingerbread Man" },
			{ "gingerbread_meleedungeon", "Gingerbread Man" },
			{ "bradleyapc", "Bradley APC" },
			{ "m2bradley", "Bradley APC" },
			{ "patrolhelicopter", "Patrol Helicopter" },
		};

		[PluginReference]
		Plugin BetterMobs;

		class TrackedTarget
		{
			public BaseCombatEntity Entity;
			public int DisplayedHealth = -1;
		}

		readonly Dictionary<ulong, TrackedTarget> targetsByPlayerId = new Dictionary<ulong, TrackedTarget>();

		void OnServerInitialized()
		{
			timer.Every(RefreshIntervalSeconds, RefreshAllBars);
		}

		void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList)
				CuiHelper.DestroyUi(player, BarPanelName);
		}

		void OnPlayerDisconnected(BasePlayer player)
		{
			targetsByPlayerId.Remove(player.userID);
		}

		void OnEntityTakeDamage(BaseCombatEntity victimEntity, HitInfo hitInfo)
		{
			var attacker = hitInfo?.InitiatorPlayer;
			if (attacker == null || attacker.IsNpc || !attacker.IsConnected || victimEntity == attacker || !IsEnemy(victimEntity))
				return;

			targetsByPlayerId[attacker.userID] = new TrackedTarget { Entity = victimEntity };
			// The damage is applied after this hook returns, so read the new health on the next tick.
			NextTick(() => RefreshBar(attacker));
		}

		static bool IsEnemy(BaseCombatEntity entity)
		{
			return entity is BasePlayer || entity is BaseNpc || entity is BaseNPC2 || entity is BradleyAPC || entity is PatrolHelicopter;
		}

		void RefreshAllBars()
		{
			foreach (var player in BasePlayer.activePlayerList)
			{
				if (targetsByPlayerId.ContainsKey(player.userID))
					RefreshBar(player);
			}
		}

		void RefreshBar(BasePlayer player)
		{
			TrackedTarget target;
			if (player == null || !targetsByPlayerId.TryGetValue(player.userID, out target))
				return;

			var entity = target.Entity;
			if (!player.IsConnected || player.IsDead() || entity == null || entity.IsDestroyed || entity.IsDead()
				|| Vector3.Distance(player.transform.position, entity.transform.position) > MaxTrackingDistance)
			{
				targetsByPlayerId.Remove(player.userID);
				CuiHelper.DestroyUi(player, BarPanelName);
				return;
			}

			int health = Mathf.CeilToInt(entity.health);
			if (health == target.DisplayedHealth)
				return;

			target.DisplayedHealth = health;
			DrawBar(player, GetDisplayName(entity), health, Mathf.CeilToInt(entity.MaxHealth()));
		}

		string GetDisplayName(BaseCombatEntity entity)
		{
			// Players and BetterMobs mobs (named after their preset) already have a proper display name.
			var victimPlayer = entity as BasePlayer;
			if (victimPlayer != null && !string.IsNullOrEmpty(victimPlayer.displayName)
				&& (!victimPlayer.IsNpc || (BetterMobs != null && BetterMobs.Call<bool>("IsMob", entity))))
				return victimPlayer.displayName;

			string prefabName = entity.ShortPrefabName;
			string knownName;
			if (NamesByShortPrefabName.TryGetValue(prefabName, out knownName))
				return knownName;

			if (prefabName.StartsWith("scientistnpc_") || prefabName.StartsWith("scientist2"))
				return "Scientist";

			return TidyPrefabName(prefabName);
		}

		// "wolf3" -> "Wolf", "some_new.entity" -> "Some New"
		static string TidyPrefabName(string prefabName)
		{
			string tidiedName = prefabName.Replace(".entity", "").TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9')
				.Replace('_', ' ').Replace('.', ' ').Trim();
			return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(tidiedName);
		}

		static void DrawBar(BasePlayer player, string targetName, int health, int maxHealth)
		{
			float healthFraction = maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;

			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				Image = { Color = "0 0 0 0.6" },
				RectTransform = { AnchorMin = "0.35 0.915", AnchorMax = "0.65 0.945" }
			}, "Hud", BarPanelName);
			container.Add(new CuiPanel
			{
				Image = { Color = "0.75 0.15 0.15 0.9" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = $"{healthFraction:0.###} 1" }
			}, BarPanelName);
			container.Add(new CuiLabel
			{
				Text = { Text = $"{targetName}  {health}/{maxHealth}", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
			}, BarPanelName);

			CuiHelper.DestroyUi(player, BarPanelName);
			CuiHelper.AddUi(player, container);
		}
	}
}
