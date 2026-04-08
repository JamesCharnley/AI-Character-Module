namespace AICharacterModule.NPC.StateMachine.Core
{
    public interface ISubStateMachine<TGlobalData>
    {
        string Name { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
        TGlobalData GlobalData { get; }

        bool IsLocked { get; }
    }
}
