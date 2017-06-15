using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DunGen2._0
{
  public class LabMapContainer
  {
    int columns, rows;
    public int[,] dungeonMap;
    public int[,] renderingMap;
    public bool[,] fogOfWarMap;
    public bool[,] walkableMap;
    public List<DungeonRoom> rooms;

    public LabMapContainer(int[,] dungeonMap, int[,] renderingMap, bool[,] fogOfWarMap, bool[,] walkableMap, List<DungeonRoom> rooms)
    {
      // TODO: Complete member initialization
      this.dungeonMap = dungeonMap;
      columns = dungeonMap.GetLength(0);
      rows = dungeonMap.GetLength(1);
      this.renderingMap = renderingMap;
      this.fogOfWarMap = fogOfWarMap;
      this.walkableMap = walkableMap;
      this.rooms = rooms;
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("\n=============Start of the dungeon==================\n");
      sb.Append(string.Format("Dungeon of {0} columns and {1} rows, containing {2} rooms.\n", columns, rows, rooms.Count));
      sb.Append("Dungeon map:\n");
      for (int x = 0; x < columns; x++)
      {
        sb.Append("\n");
        for (int y = 0; y < rows; y++)
        {
          switch (dungeonMap[x, y])
          {
            case DungeonGenerator.SECRET_DOOR_TILE:
              sb.Append("S ");
              break;
            case DungeonGenerator.RADIATION_TILE:
              sb.Append("R ");
              break;
            case DungeonGenerator.COLUMN_TILE:
              sb.Append("* ");
              break;
            case DungeonGenerator.KEY_LOCKED_DOOR_TILE:
              sb.Append("K ");
              break;
            case DungeonGenerator.COLLAPSED_DOOR_TILE:
              sb.Append("C ");
              break;
            case DungeonGenerator.WALL_TILE:
            case DungeonGenerator.SURROUNDING_WALL_TILE:
              sb.Append("  ");
              break;
            case DungeonGenerator.VERTICAL_WALL_TILE:
            case DungeonGenerator.HORIZONTAL_WALL_TILE:
            case DungeonGenerator.CORNER_TILE:
              sb.Append(1 + " ");
              break;
            case DungeonGenerator.DOOR_TILE:
              sb.Append("= ");
              break;
            case DungeonGenerator.DYNAMITE:
              sb.Append("X ");
              break;
            case DungeonGenerator.KEY:
              sb.Append("V ");
              break;
            case DungeonGenerator.KEY_ROOM_LOOT:
            case DungeonGenerator.RADIATION_LOOT:
            case DungeonGenerator.RANDOM_LOOT:
              sb.Append("L ");
              break;
            default:
              sb.Append(dungeonMap[x, y].ToString() + " ");
              break;
          }
        }
      }
      sb.Append("\n=============End of the dungeon==================\n");
      return sb.ToString();
    }
  }
}
