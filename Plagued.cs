using System.Reflection;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Plagued", "Wernesgruner", 0.1)]
    [Description("Everyone is infected.")]

    class Plagued : RustPlugin
    {
        
        private static int playerLayer;

        // Get the buffer size from the Vis class
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic)).GetValue(null);

        void OnServerInitialized()
        {
            //playerLayer = LayerMask.GetMask("Player (Server)");
            playerLayer = LayerMask.GetMask("AI");
        }

        void OnPlayerInit(BasePlayer player)
        {
            player.gameObject.AddComponent<ProximityDetector>();
            Puts(player.displayName + " has been plagued!");
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            player.gameObject.GetComponent<ProximityDetector>().disableProximityCheck();
            Puts(player.displayName + " has been unplagued!");
        }

        void OnPlayerProximity(BasePlayer player, BasePlayer[] players)
        {
            Puts(player.displayName + " is close to " + players.Length.ToString() + " other players!");
        }

        public class ProximityDetector : MonoBehaviour
        {
            public BasePlayer player;

            public void disableProximityCheck()
            {
                CancelInvoke("CheckProximity");
            }

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                InvokeRepeating("CheckProximity", 2f, 1.5f);
            }

            void OnDestroy()
            {
                disableProximityCheck();
            }

            void CheckProximity()
            {
                var count = Physics.OverlapSphereNonAlloc(player.transform.position, 30, colBuffer, playerLayer);

                if (count > 0)
                {
                    BasePlayer[] playersNear = new BasePlayer[count];
                    for (int i = 0; i < count; i++)
                    {
                        var collider = colBuffer[i];
                        colBuffer[i] = null;
                        var collidingPlayer = collider.GetComponentInParent<BasePlayer>();
                        playersNear[i] = collidingPlayer;
                    }
                    notifyPlayerProximity(playersNear);
                } else
                {
                    notifyPlayerAlone();
                }
            }

            void notifyPlayerProximity(BasePlayer[] players)
            {
                Interface.Oxide.CallHook("OnPlayerProximity", player, players);
            }

            void notifyPlayerAlone()
            {
                Interface.Oxide.CallHook("OnPlayerAlone", player);
            }
        }
    }
}