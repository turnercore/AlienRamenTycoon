namespace Project
{
    // The valid command types that the application can interpret as the user intent. Does not include cursor movement
    enum CommandTypes
    {
        ReadyForNextDay,
        CancelReadyForNextDay,
        UnlockScrapUpgrade,
        UnlockTile,
        BuyBuildingOnTile,
        SellBuildingOnTile,
        UpgradeBuildingOnTile,
        UpgradeShip,
        Disconnect,
        Resync,
    }
}
