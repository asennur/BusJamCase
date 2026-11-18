using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Geçici: Z3'te GameManager devralana kadar bir leveli kurar ve erişilebilirliği uygular.</summary>
    public class BoardDebugBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelData level;
        [SerializeField] private BoardView boardView;

        private readonly Board board = new Board();

        private void Start()
        {
            board.Setup(level);
            boardView.Build(level, board);

            var reachable = new bool[board.CellCount];
            board.ComputeReachable(reachable);
            for (int i = 0; i < reachable.Length; i++)
            {
                Passenger passenger = board.GetOccupant(i);
                if (passenger != null)
                    passenger.SetReachable(reachable[i]);
            }
        }
    }
}
