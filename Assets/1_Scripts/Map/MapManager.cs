using System.Linq;
using UnityEngine;

namespace Map
{
    public class MapManager : MonoBehaviour
    {
        public MapConfig config;
        public MapView view;

        public Map CurrentMap { get; private set; }

        private void Start()
        {
            bool isResumingBattle = SaveRun.HasActiveBattle();
            if (SaveRun.HasMap())
            {
                Map map = SaveRun.LoadMap();
                // using this instead of .Contains()
                if (map != null && map.path.Any(p => p.Equals(map.GetBossNode().point)))
                {
                    // payer has already reached the boss, generate a new map
                    GenerateNewMap();
                }
                else
                {
                    CurrentMap = map;
                    // player has not reached the boss yet, load the current map
                    if (map != null)
                    {
                        // If we are resuming directly into a battle, do not show the map UI on start.
                        // LevelMap will kick off the battle and hide map visuals.
                        if (!isResumingBattle)
                        {
                            view.ShowMap(map);
                        }
                        else
                        {
                            // Ensure map visuals are hidden even if something else tries to show them.
                            if (view != null)
                            {
                                view.HideMap();
                                view.gameObject.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        GenerateNewMap();
                    }
                }
            }
            else
            {
                GenerateNewMap();
            }
        }

        public void GenerateNewMap()
        {
            Map map = MapGenerator.GetMap(config);
            CurrentMap = map;
            Debug.Log(map.ToJson());
            view.ShowMap(map);
        }

        public void SaveMap()
        {
            if (CurrentMap == null) return;
            SaveRun.SaveMap(CurrentMap);
        }

        private void OnApplicationQuit()
        {
            SaveMap();
        }
    }
}
