using System.Collections.Generic;
using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Dokunma kararının tek yeri: erişilebilirlik kontrolü, hücreyi boşaltma, koşu başlatma.
    /// Bu pakette hedef geçici olarak grid dışındaki sabit çıkış noktası; otobüs/slot hedefi Z5'te gelir.
    /// </summary>
    public class GameplayController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private BoardView boardView;
        [SerializeField] private float runSpeed = 4f;

        private Board board;
        private bool[] reachable;
        private readonly List<Vector2Int> pathBuffer = new List<Vector2Int>();
        private readonly List<Vector3> worldPathBuffer = new List<Vector3>();

        private void OnEnable()
        {
            gameManager.LevelLoaded += OnLevelLoaded;
            inputHandler.PassengerTapped += OnPassengerTapped;
        }

        private void OnDisable()
        {
            gameManager.LevelLoaded -= OnLevelLoaded;
            inputHandler.PassengerTapped -= OnPassengerTapped;
        }

        // Z3'te bootstrap silinince ilk outline uygulaması boşta kaldı; her level yüklenince burada yapılır.
        private void OnLevelLoaded(int levelNumber, LevelData level)
        {
            board = gameManager.Board;
            if (reachable == null || reachable.Length < board.CellCount)
                reachable = new bool[board.CellCount];

            RefreshReachable();
        }

        private void OnPassengerTapped(Passenger passenger)
        {
            if (gameManager.State != GameState.Playing || passenger.State != PassengerState.Idle)
                return;

            if (!reachable[board.ToIndex(passenger.Cell)])
            {
                passenger.ShowAngry();
                return;
            }

            Vector2Int startCell = passenger.Cell;
            board.SetOccupant(startCell, null);
            RefreshReachable();

            board.TryFindPath(startCell, pathBuffer);
            Vector2Int lastCell = pathBuffer.Count > 0 ? pathBuffer[pathBuffer.Count - 1] : startCell;

            worldPathBuffer.Clear();
            for (int i = 0; i < pathBuffer.Count; i++)
                worldPathBuffer.Add(boardView.CellToWorld(pathBuffer[i]));
            worldPathBuffer.Add(boardView.CellToWorld(new Vector2Int(lastCell.x, -1)));

            passenger.RunTo(worldPathBuffer, runSpeed, null);
        }

        private void RefreshReachable()
        {
            board.ComputeReachable(reachable);
            for (int i = 0; i < board.CellCount; i++)
            {
                Passenger p = board.GetOccupant(i);
                if (p != null)
                    p.SetReachable(reachable[i]);
            }
        }
    }
}
