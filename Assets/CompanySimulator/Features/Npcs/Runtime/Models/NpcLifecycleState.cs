namespace CompanySimulator.Features.Npcs.Runtime.Models
{
    public enum NpcLifecycleState
    {
        None = 0,
        PendingSpawn = 1,
        Spawned = 2,
        Walking = 3,
        Seated = 4,
        Interviewing = 5,
        Leaving = 6,
        Dismissed = 7
    }
}
