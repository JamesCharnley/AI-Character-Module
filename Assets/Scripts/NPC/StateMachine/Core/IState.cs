namespace AICharacterModule.NPC.StateMachine.Core
{
    public interface IState<TLocalData, TGlobalData>
    {
        void Enter(TLocalData localData, TGlobalData globalData);
        void Tick(TLocalData localData, TGlobalData globalData, float deltaTime);
        void Exit(TLocalData localData, TGlobalData globalData);
    }
}
