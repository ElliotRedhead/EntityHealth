namespace Oxide.Plugins
{
	[Info("EntityHealthBar", "ExpertRaptor", "0.0.1")]
	[Description("Display health bar for attacked entities.")]
	class EntityHealthBar : RustPlugin
	{
		void OnEntityTakeDamage(BaseCombatEntity victimEntity, HitInfo hitInfo)
		{
			if (victimEntity is BasePlayer)
				return;
			if (hitInfo?.InitiatorPlayer is BasePlayer)
			{
				Puts(victimEntity.startHealth.ToString());
				Puts(victimEntity._health.ToString());
				Puts(victimEntity.healthFraction.ToString());
			}
		}
	}
}
