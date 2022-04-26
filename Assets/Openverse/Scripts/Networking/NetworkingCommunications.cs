namespace Openverse.NetCode
{
    public enum ServerToClientId : ushort
    {
        downloadWorld = 1,
        openWorld,
        spawnPlayer,
        playerLocation,
        spawnObject,
        updateObject,
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