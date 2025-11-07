namespace Omni.Core.Components
{
    enum TransformMovementState
    {
        Idle,
        StartedMoving,
        Moving
    }

    public enum AuthorityMode
    {
        Owner, Server
    }

    public enum UpdateMode
    {
        Update,
        FixedUpdate
    }
}