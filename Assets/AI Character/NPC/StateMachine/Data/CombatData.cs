namespace AICharacterModule.NPC.StateMachine.Data
{
    /// <summary>
    /// Data local to the combat sub-state machine.
    /// </summary>
    public class CombatData
    {
        public float CooldownSeconds = 1f;
        public float CooldownTimer;
        public float DamagePerHit = 15f;
    }
}
