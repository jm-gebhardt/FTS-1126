namespace Gebhardt.Acc.IntegrationTest.Models;

public enum ContainerState
{
    Unknown,
    Created,
    Restarting,
    Running,
    Removing,
    Paused,
    Exited,
    Dead
}