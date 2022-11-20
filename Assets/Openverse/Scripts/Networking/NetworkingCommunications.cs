namespace Openverse.NetCode
{
    public enum ServerToClientId : ushort
    {
        spawnPlayer = 1,
        playerLocation,
        spawnObject,
        updateVariable,
        transformObject,
        addComponent,
        removeComponent,
        moveClientMoveable,
        RequestInput
    }

    public enum ClientToServerId : ushort
    {
        playerName = 1,
        vrPositions,
        playerReady,
        moveClientMoveable,
        supplyInput
    }
}