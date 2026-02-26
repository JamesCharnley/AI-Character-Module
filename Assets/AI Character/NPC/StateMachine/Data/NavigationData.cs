using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.Data
{
    /// <summary>
    /// Data local to the navigation sub-state machine.
    /// </summary>
    public class NavigationData
    {
        public Vector3 PatrolPoint;
        public float ReachedThreshold = 0.25f;
    }
}
