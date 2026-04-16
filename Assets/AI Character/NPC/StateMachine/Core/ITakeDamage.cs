using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.Core
{
    public interface ITakeDamage
    {
        void TakeDamage(float _amount);
        void TakeDamage(float _amount, Vector3 _direction, Vector2 _offset);
    }
}
