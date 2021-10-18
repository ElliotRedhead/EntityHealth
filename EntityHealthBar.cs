namespace Oxide.Plugins
{
	[Info("EntityHealthBar", "ExpertRaptor", "0.0.1")]
	[Description("Display health bar for attacked entities.")]
	class EntityHealthBar : RustPlugin
	{
		void NotifyEntityStateInChat(BasePlayer recipient, BaseCombatEntity victimEntity, int damage, float distance)
		{
			int victimMaxHealth = (int)victimEntity.startHealth;
			int victimPreviousHealth = (int)victimEntity._health;
			int victimResultingHealth = victimPreviousHealth - damage;
			string distanceString = distance.ToString("0.00");

			recipient.ChatMessage($"{victimResultingHealth.ToString()}/{victimMaxHealth.ToString()}, {distanceString}");
		}
		void OnEntityTakeDamage(BaseCombatEntity victimEntity, HitInfo hitInfo)
		{
			if (victimEntity is BasePlayer && !(hitInfo.Initiator is BasePlayer))
				return;

			var attacker = hitInfo.InitiatorPlayer;
			if (hitInfo != null && attacker is BasePlayer)
			{
				float damageFloat = hitInfo.damageTypes.Get(hitInfo.damageTypes.GetMajorityDamageType());
				int damage = (int)hitInfo.damageTypes.Total();
				float distance = hitInfo.ProjectileDistance;
				NotifyEntityStateInChat(attacker, victimEntity, damage, distance);

			}
		}
	}
}
