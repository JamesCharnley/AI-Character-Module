using System.Collections.Generic;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.Data
{
    /// <summary>
    /// Data local to the navigation sub-state machine.
    /// </summary>
    public class NavigationData
    {
        private NPCGlobalData GlobalData;

        public Vector3 PatrolPoint;
        public float ReachedThreshold = 3.0f;
     

        public NavigationData(NPCGlobalData _globalData)
        {
            GlobalData = _globalData;
        }

        

  
        
    }
}
