using System.Reflection;
using System.Collections.Generic;

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
        private static int plagueRange = 20;
        private static int plagueMinAffinity = 20;
        private static int affinityIncRate = 10;
        private static int affinityDecRate = 1;
        private static int playerLayer;

        // Get the buffer size from the Vis class
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic)).GetValue(null);

        //
        private Dictionary<ulong, PlayerState> playerStates;

        void OnServerInitialized()
        {
            playerLayer = LayerMask.GetMask("Player (Server)");
            //playerLayer = LayerMask.GetMask("AI");

            // Reload the player states
            playerStates = new Dictionary<ulong, PlayerState>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                playerStates.Add(player.userID, new PlayerState(player));
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            player.gameObject.AddComponent<ProximityDetector>();

            if (!playerStates.ContainsKey(player.userID))
            {
                playerStates.Add(player.userID, new PlayerState(player));
                Puts(player.displayName + " has been plagued!");
            } else
            {
                Puts(player.displayName + " has returned!");
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            ProximityDetector proximityDetector = player.gameObject.GetComponent<ProximityDetector>();
            proximityDetector.disableProximityCheck();
            Puts(player.displayName + " is no longer watched!");
        }

        void OnPlayerProximity(BasePlayer player, BasePlayer[] players)
        {
            if (playerStates.ContainsKey(player.userID))
            {
                playerStates[player.userID].increasePlaguePenalty(players);
                //Puts(player.displayName + " is close to " + (players.Length - 1).ToString() + " other players!");
            } else
            {
                Puts(player.displayName + " has no state!");
            }
        }

        void OnPlayerAlone(BasePlayer player)
        {
            if (playerStates.ContainsKey(player.userID))
            {
                playerStates[player.userID].decreasePlaguePenalty();
            }
            else
            {
                Puts(player.displayName + " has no state!");
            }
        }

        public class PlayerState
        {
            private BasePlayer player;
            private int plagueLevel;
            private int narcolepsyEffect;
            private int nauseaEffect;
            private int bleedingEffect;
            private Dictionary<ulong, int> associates;
            private bool pristine;

            public PlayerState(BasePlayer newPlayer)
            {
                player = newPlayer;
                plagueLevel = 0;
                associates = new Dictionary<ulong, int>();
            }

            public int increaseAssociateAffinity(BasePlayer associate)
            {
                if (associate == null) return -1;
                if (player.userID == associate.userID) return -1;

                if (associates.ContainsKey(associate.userID))
                {
                    if ((associates[associate.userID] + affinityIncRate) < int.MaxValue) associates[associate.userID] += affinityIncRate;
                }
                else
                {
                    associates.Add(associate.userID, 0);
                }

                Interface.Oxide.LogInfo(player.displayName + " -> " + associate.displayName + " = " + associates[associate.userID].ToString());

                return associates[associate.userID];
            }

            public void increasePlaguePenalty(BasePlayer[] associates)
            {
                int contagionVectorsCount = 0;

                foreach (BasePlayer associate in associates)
                {
                    int affinity = increaseAssociateAffinity(associate);

                    if (affinity >= plagueMinAffinity)
                    {
                        contagionVectorsCount++;
                    }
                }

                if (contagionVectorsCount > 0)
                {
                    increasePlagueLevel(contagionVectorsCount);
                } else
                {
                    decreasePlagueLevel();
                }
            }

            public void decreasePlaguePenalty()
            {
                if (pristine) return;

                List<ulong> keys = new List<ulong>(associates.Keys);

                foreach (ulong key in keys)
                {
                    //if ((associates[key] - affinityDecRate) >= 0) associates[key] -= affinityDecRate;
                    if ((associates[key] - affinityDecRate) >= 0)
                    {
                        associates[key] = associates[key] - affinityDecRate;
                    }
                }
            }

            public void increasePlagueLevel(int contagionVectorCount)
            {

            }

            public void decreasePlagueLevel()
            {

            }
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
                var count = Physics.OverlapSphereNonAlloc(player.transform.position, plagueRange, colBuffer, playerLayer);

                if (count > 1)
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