using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;

namespace HiveUplink.SandboxHack
{
    public class Player
    {
        public static long TryGetIdentityId(ulong steamId, string playerName)
        {
            long id = MySession.Static.Players.TryGetIdentityId(steamId);
            if (id == 0L)
                id = createSandboxEntry(steamId, playerName, "Default_Astronaut");

            return id;
        }

        private static long createSandboxEntry(ulong steamId, string playerName, string characterModel, bool realPlayer = true)
        {
            MyNetworkClient steamClient = new MyNetworkClient(steamId);
            MyPlayer.PlayerId playerId = new MyPlayer.PlayerId(steamId, 0);
            MyIdentity myIdentity = MySession.Static.Players.TryGetPlayerIdentity(playerId);
            if (myIdentity == null)
                myIdentity = MySession.Static.Players.RespawnComponent.CreateNewIdentity(playerName, playerId, characterModel);

            MyPlayer player = MySession.Static.Players.CreateNewPlayer(myIdentity, steamClient, myIdentity.DisplayName, realPlayer);
            MySession.Static.Players.RemovePlayer(player);
            return MyAPIGateway.Players.TryGetIdentityId(steamId);
        }
    }
}
