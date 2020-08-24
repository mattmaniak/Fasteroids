using UnityEngine;

public struct AsteroidDto
{
    public Vector3 Position;
    public float TimeLeftToRespawn;
    public float RotationSpeed;
    internal Vector3 Direction;
    internal float Speed;
    internal bool DestroyedThisFrame;
}
