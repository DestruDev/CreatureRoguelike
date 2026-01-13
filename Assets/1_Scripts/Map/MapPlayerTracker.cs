using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Map
{
    public class MapPlayerTracker : MonoBehaviour
    {
        public bool lockAfterSelecting = false;
        public float enterNodeDelay = 1f;
        public MapManager mapManager;
        public MapView view;

        public static MapPlayerTracker Instance;

        public bool Locked { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SelectNode(MapNode mapNode)
        {
            if (Locked) return;

            // Debug.Log("Selected node: " + mapNode.Node.point);

            if (mapManager.CurrentMap.path.Count == 0)
            {
                // player has not selected the node yet, he can select any of the nodes with y = 0
                if (mapNode.Node.point.y == 0)
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
            else
            {
                Vector2Int currentPoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
                Node currentNode = mapManager.CurrentMap.GetNode(currentPoint);

                if (currentNode != null && currentNode.outgoing.Any(point => point.Equals(mapNode.Node.point)))
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
        }

        private void SendPlayerToNode(MapNode mapNode)
        {
            // Lock node selection immediately to prevent selecting multiple nodes in quick succession
            Locked = true;
            mapManager.CurrentMap.path.Add(mapNode.Node.point);
            mapManager.SaveMap();
            view.SetAttainableNodes();
            view.SetLineColors();
            mapNode.ShowSwirlAnimation();

            // Hide map after delay, then enter node
            DOTween.Sequence()
                .AppendInterval(enterNodeDelay)
                .OnComplete(() =>
                {
                    // Hide the map before entering the node
                    HideMap();
                    EnterNode(mapNode);
                });
        }
        
        /// <summary>
        /// Hides the map view by hiding the OuterMapParent
        /// </summary>
        private void HideMap()
        {
            // Hide the OuterMapParent that contains all map visuals
            if (view != null)
            {
                view.HideMap();
            }
            else
            {
                Debug.LogWarning("MapPlayerTracker: MapView not found! Cannot hide map.");
            }
        }

        private void EnterNode(MapNode mapNode)
        {
            // we have access to blueprint name here as well
            Debug.Log("Entering node: " + mapNode.Node.blueprintName + " of type: " + mapNode.Node.nodeType);
            
            // Find LevelMap and start the appropriate level
            LevelMap levelMap = FindFirstObjectByType<LevelMap>();
            if (levelMap != null)
            {
                // Start level based on node type
                levelMap.StartLevelFromNodeType(mapNode.Node.nodeType);
                
                // Keep locked until map is shown again - unlock will happen in ShowMapView()
            }
            else
            {
                Debug.LogWarning("MapPlayerTracker: LevelMap not found! Cannot start level.");
                // Unlock anyway so player isn't stuck if LevelMap is missing
                Locked = false;
            }
        }
        
        /// <summary>
        /// Unlocks node selection. Called when the map is shown again after completing a level.
        /// </summary>
        public void UnlockNodeSelection()
        {
            Locked = false;
        }

        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("Selected node cannot be accessed");
        }
    }
}