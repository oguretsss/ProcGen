using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DunGen2._0
{
  public class DungeonGenerator
  {

    int dungNum = 0;
    readonly int minRoomWidth = 4;
    readonly int maxRoomWidth = 8;
    readonly int minRoomHeight = 3;
    readonly int maxRoomHeight = 7;

    //used in calculating dungeon's dimensions
    private const float dungeonStretchCoeff = 7.1f;
    private const int roomSurroundings = 2;

    int[,] dungeonMap;
    int[,] interactiveObjects;

    #region Constants

    //Main Tiles
    public const int FLOOR_TILE_0 = 0;
    public const int VERTICAL_WALL_TILE = 1;
    public const int HORIZONTAL_WALL_TILE = 2;
    public const int CORNER_TILE = 3;
    public const int FOG_TRIGGER_TILE = 4;
    public const int DOOR_TILE = 5;
    public const int SURROUNDING_WALL_TILE = 6;
    public const int START_TILE = 7;
    public const int EXIT_TILE = 8;
    public const int WALL_TILE = 9;
    public const int SECRET_DOOR_TILE = 10;
    public const int KEY_LOCKED_DOOR_TILE = 12;
    public const int COLLAPSED_DOOR_TILE = 13;
    public const int UNLOCKED_DOOR_TILE = 14;
    public const int CLEARED_DOOR_TILE = 15;
    public const int COLUMN_TILE = 16;
    public const int BROKEN_TERMINAL_TILE = 198;
    public const int RADIATION_TILE = 17;

    public const int TERMINAL_TILE = 19;

    public const int DOCUMENT_TILE = 20;
    public const int RANDOM_LOOT = 21;
    public const int KEY = 22;
    public const int KEY_ROOM_LOOT = 23;
    public const int RADIATION_LOOT = 24;
    public const int DYNAMITE = 25;



    #endregion
    public bool[,] fogOfWarMap, walkableMap;
    public int[,] renderingMap;

    int columns, rows;
    List<DungeonRoom> rooms;
    //	List<DungeonDoor> doors = new List<DungeonDoor>();

    public DungeonRoom startRoom, exitRoom;
    public List<string> DungeonObjects { get; set; }
    private readonly int MAX_COLUMNS_IN_ROOM = 3;
    private readonly int MAX_RADS_IN_ROOM = 3;
    /// <summary>
    /// Creates the simple dungeon map with size depending on the amount of rooms.
    /// </summary>
    /// <returns>LabMapContainer object</returns>
    /// <param name="roomsAmount">How many rooms will the dungeon have.</param>
    /// /// <param name="hasExitToNextFloor">True if this dungeon is not the last floor of the dungeon system.</param>
    /// /// <param name="specialRooms">Optional array of rooms with custom RoomType</param>
    public LabMapContainer CreateSimpleDungeonMap(int roomsAmount, bool hasExitToNextFloor, int dungNum, params DungeonRoom[] specialRooms)
    {
      this.dungNum = dungNum;
      //Assess approximatie dungeon dimensions
      int approxDungeonArea = (int)(dungeonStretchCoeff * (roomsAmount + specialRooms.Length) * ((minRoomWidth * minRoomHeight) + (maxRoomWidth * maxRoomHeight)) / 2);
      columns = (int)Math.Sqrt(approxDungeonArea);
      rows = columns;
      fogOfWarMap = new bool[columns, rows];
      dungeonMap = new int[columns, rows];
      interactiveObjects = new int[columns, rows];
      rooms = new List<DungeonRoom>();
      DungeonObjects = new List<string>();
      //fill dungeon with walls
      for (int x = 0; x < columns; x++)
      {
        for (int y = 0; y < rows; y++)
        {
          dungeonMap[x, y] = WALL_TILE;
        }
      }

      //create rooms array and place them on map
      for (int i = 0; i < roomsAmount; i++)
      {
        DungeonRoom roomToAdd = GenerateNewRoom();
        rooms.Add(roomToAdd);
        PlaceRoomOnMap(roomToAdd);
      }


      foreach (DungeonRoom specialRoom in specialRooms)
      {
        DungeonRoom targetRoom = GenerateNewRoom();
        if (targetRoom == null)
        {
          Console.WriteLine("Couldn't find a place for a secret room =(((");
          continue;
        }
        targetRoom.roomType = specialRoom.roomType;
        rooms.Add(targetRoom);
        PlaceRoomOnMap(targetRoom);
      }
      PlaceDoorsOnMap();


      int startRoomNumber = Program.rand.Next(0, rooms.Count);
      while (rooms[startRoomNumber].roomType != RoomType.RegularRoom)
      {
        startRoomNumber = Program.rand.Next(0, rooms.Count);
      }
      startRoom = rooms[startRoomNumber];
      dungeonMap[startRoom.CenterX, startRoom.CenterY] = START_TILE;

      if (hasExitToNextFloor)
      {
        int exitRoomNumber = Program.rand.Next(0, rooms.Count);
        while (exitRoomNumber == startRoomNumber || rooms[exitRoomNumber].roomType != RoomType.RegularRoom)
        {
          exitRoomNumber = Program.rand.Next(0, rooms.Count);
        }
        exitRoom = rooms[exitRoomNumber];
        dungeonMap[exitRoom.CenterX, exitRoom.CenterY] = EXIT_TILE;
      }

      FillDungeonWithInteractiveObjects();

      renderingMap = CreateRenderingMap(dungeonMap);
      walkableMap = CreateWalkableMap(renderingMap);
      //return CreateRenderingMap(dungeonMap);
      return new LabMapContainer(dungeonMap, renderingMap, fogOfWarMap, walkableMap, rooms);
    }

    bool AbleToPlace(DungeonRoom room)
    {
      int count = 0;
      for (int x = room.UpperLeftX - roomSurroundings; x < room.LowerRightX + roomSurroundings; x++)
      {
        for (int y = room.UpperLeftY - roomSurroundings; y < room.LowerRightY + roomSurroundings; y++)
        {
          if (dungeonMap[x, y] != WALL_TILE)
          {
            count++;
            return false;
          }
          else count++;
        }
      }
      return true;
    }

    DungeonRoom GenerateNewRoom()
    {
      int roomX = Program.rand.Next(4, columns - maxRoomWidth - 4);
      int roomY = Program.rand.Next(4, rows - maxRoomHeight - 4);
      int roomWidth = Program.rand.Next(minRoomWidth, maxRoomWidth);
      int roomHeight = Program.rand.Next(minRoomHeight, maxRoomHeight);
      DungeonRoom targetRoom = new DungeonRoom(roomX, roomY, roomWidth, roomHeight);
      int iterations = 0;
      bool foundPlace = true;
      while (!AbleToPlace(targetRoom))
      {
        roomX = Program.rand.Next(4, columns - maxRoomWidth - 4);
        roomY = Program.rand.Next(4, rows - maxRoomHeight - 4);
        targetRoom = new DungeonRoom(roomX, roomY, roomWidth, roomHeight);
        iterations++;
        if (iterations > 14000000)
        {
          foundPlace = false;
          break;
          //return null;
        }
      }
      if (!foundPlace)
      {
        roomWidth = minRoomWidth;
        roomHeight = minRoomHeight;
        for (int x = 4; x < columns - maxRoomWidth - 4; x++)
        {
          for (int y = 4; y < rows - maxRoomHeight - 4; y++)
          {
            targetRoom = new DungeonRoom(x, y, roomWidth, roomHeight);
            if(AbleToPlace(targetRoom))
            {
              foundPlace = true;
              Console.WriteLine("Found some placy! For dungeon number: "+ dungNum);
              return targetRoom;
            }
          }

        }
      }
      else
        return targetRoom;
      if(!foundPlace)
      {
        Console.WriteLine("Still couldn't find place for dungeon {0}! what the fuck!!", dungNum);
        return null;
      }
      else
      {
        Console.WriteLine("ALARM! THIS CANNOT HAPPEN!");
        return null;
      }
    }
    void PlaceRoomOnMap(DungeonRoom room)
    {
      for (int x = room.UpperLeftX - roomSurroundings; x < room.LowerRightX + roomSurroundings; x++)
      {
        for (int y = room.UpperLeftY - roomSurroundings; y < room.LowerRightY + roomSurroundings; y++)
        {
          if (x == room.UpperLeftX - roomSurroundings || x == room.LowerRightX + (roomSurroundings - 1))
          {
            dungeonMap[x, y] = SURROUNDING_WALL_TILE;
          }
          else if (y == room.UpperLeftY - roomSurroundings || y == room.LowerRightY + (roomSurroundings - 1))
          {
            dungeonMap[x, y] = SURROUNDING_WALL_TILE;
          }
          else if (x == room.UpperLeftX - (roomSurroundings - 1) || x == room.LowerRightX)
          {
            dungeonMap[x, y] = VERTICAL_WALL_TILE;
          }
          else if (y == room.UpperLeftY - (roomSurroundings - 1) || y == room.LowerRightY)
          {
            dungeonMap[x, y] = HORIZONTAL_WALL_TILE;
          }
          else
          {
            switch (room.roomType)
            {
              case RoomType.RegularRoom:
                dungeonMap[x, y] = FLOOR_TILE_0;
                break;
              case RoomType.RadiationRoom:
                dungeonMap[x, y] = RADIATION_TILE;
                break;
              default:
                dungeonMap[x, y] = FLOOR_TILE_0;
                break;
            }
          }
          dungeonMap[room.UpperLeftX - 1, room.UpperLeftY - 1] = CORNER_TILE;
          dungeonMap[room.UpperLeftX - 1, room.LowerRightY] = CORNER_TILE;
          dungeonMap[room.LowerRightX, room.UpperLeftY - 1] = CORNER_TILE;
          dungeonMap[room.LowerRightX, room.LowerRightY] = CORNER_TILE;
        }
      }
      
      if (room.roomType == RoomType.RadiationRoom) PlaceObjectInsideRoom(FLOOR_TILE_0, room);

      int columns = Program.rand.Next(MAX_COLUMNS_IN_ROOM);
      if (room.roomType != RoomType.RadiationRoom)
      {
        for (int i = 0; i < columns; i++)
        {
          PlaceObjectInsideRoom(COLUMN_TILE, room);
        }

        int radTiles = Program.rand.Next(MAX_RADS_IN_ROOM);
        for (int i = 0; i < radTiles; i++)
        {
          PlaceObjectInsideRoom(RADIATION_TILE, room);
        } 
      }

      AddRoomToDungeon(room);
    }

    DungeonRoom FindNearestRoom(DungeonRoom target)
    {
      if (rooms.Count <= 1)
      {
        return null;
      }
      else
      {
        int targetX = target.CenterX;
        int targetY = target.CenterY;
        int nearestDistance = 0;
        DungeonRoom nearestRoom = null;
        foreach (DungeonRoom room in rooms)
        {
          int distance = (int)(Math.Abs(room.CenterX - target.CenterX) + Math.Abs(room.CenterY - target.CenterY));
          if (nearestDistance != 0 && (room.CenterX != targetX || room.CenterY != targetY))
          {
            if (distance < nearestDistance)
            {
              nearestDistance = distance;
              nearestRoom = room;
            }
          }
          else if (nearestDistance == 0 && (room.CenterX != targetX || room.CenterY != targetY))
          {
            nearestDistance = (int)(Math.Abs(room.CenterX - target.CenterX) + Math.Abs(room.CenterY - target.CenterY));
            nearestRoom = room;
          }
        }
        return nearestRoom;
      }
    }

    public DungeonRoom FindClosestToCenterRoom()
    {
      int dungeonCenterX = dungeonMap.GetLength(0) / 2;
      int dungeonCenterY = dungeonMap.GetLength(1) / 2;
      int distance = 0;
      bool roomFound = false;
      DungeonRoom closestRoom = null;
      foreach (DungeonRoom room in rooms)
      {
        if (!roomFound)
        {
          distance = Math.Abs(room.CenterX - dungeonCenterX) + Math.Abs(room.CenterY - dungeonCenterY);
          closestRoom = room;
          roomFound = true;
        }
        else
        {
          int currentDistance = Math.Abs(room.CenterX - dungeonCenterX) + Math.Abs(room.CenterY - dungeonCenterY);
          if (currentDistance <= distance)
          {
            closestRoom = room;
          }
        }
      }
      return closestRoom;
    }

    void ConnectRoomWithCell(DungeonRoom room1, int targetX, int targetY)
    {
      int x1 = room1.CenterX;
      int y1 = room1.CenterY;
      int x2 = targetX;
      int y2 = targetY;

      int xDir = (x2 > x1) ? 1 : -1;
      int yDir = (y2 > y1) ? 1 : -1;

      //horizontal corridor
      for (int x = x1; x != x2 + xDir; x += xDir)
      {
        if (dungeonMap[x, y1] == CORNER_TILE || dungeonMap[x, y1] == HORIZONTAL_WALL_TILE)
        {
          y1 += yDir;
          dungeonMap[x - xDir, y1] = FLOOR_TILE_0;
          dungeonMap[x, y1] = FLOOR_TILE_0;
        }
        else
        {
          if (dungeonMap[x, y1] == FOG_TRIGGER_TILE || dungeonMap[x, y1] == SECRET_DOOR_TILE || dungeonMap[x, y1] == RADIATION_TILE)
          {
            continue;
          }
          else
          {
            if (dungeonMap[x, y1] == VERTICAL_WALL_TILE && room1.roomType == RoomType.SecretRoom)
            {
              dungeonMap[x, y1] = SECRET_DOOR_TILE;
            }
            else
            {
              dungeonMap[x, y1] = FLOOR_TILE_0;
            }
          }
        }
      }

      //vertical corridor
      for (int y = y1; y != y2 + yDir; y += yDir)
      {
        if (dungeonMap[x2, y] == CORNER_TILE || dungeonMap[x2, y] == VERTICAL_WALL_TILE)
        {
          x2 += xDir;
          dungeonMap[x2, y - yDir] = FLOOR_TILE_0;
          dungeonMap[x2, y] = FLOOR_TILE_0;
        }
        else
        {
          if (dungeonMap[x2, y] == FOG_TRIGGER_TILE || dungeonMap[x2, y] == SECRET_DOOR_TILE || dungeonMap[x2, y] == RADIATION_TILE)
          {
            continue;
          }
          else
          {
            if (dungeonMap[x2, y] == HORIZONTAL_WALL_TILE && room1.roomType == RoomType.SecretRoom)
            {
              dungeonMap[x2, y] = SECRET_DOOR_TILE;
            }
            else
            {
              dungeonMap[x2, y] = FLOOR_TILE_0;
            }
          }
        }
      }

      // dungeonMap[x2, y1] = FOG_TRIGGER_TILE;
      //dungeonMap[x2, y2] = FOG_TRIGGER_TILE;
    }
    /// <summary>
    /// Adds the room to the dungeon and connects it with the closest dungeon cell.
    /// </summary>
    /// <param name="target">Room to add</param>
    void AddRoomToDungeon(DungeonRoom target)
    {
      if (rooms.Count < 2) return;
      int[] closestDungeonCell = { 9999, 9999 };
      int closestDistance = Math.Abs(closestDungeonCell[0] - target.CenterX) + Math.Abs(closestDungeonCell[1] - target.CenterY);
      for (int x = 0; x < columns; x++)
      {
        for (int y = 0; y < rows; y++)
        {
          if (CellInsideRoomBounds(target, x, y) || (dungeonMap[x, y] != FLOOR_TILE_0 && dungeonMap[x, y] != DOOR_TILE))
          {
            continue;
          }
          else
          {
            int distance = Math.Abs(x - target.CenterX) + Math.Abs(y - target.CenterY);
            if (distance <= closestDistance)
            {
              if (target.roomType == RoomType.SecretRoom)
              {
                //Check that secret rooms are attached only to other rooms, not to coridors
                if (!CellInsideAnyRoom(x, y))
                {
                  continue;
                }
              }
              closestDistance = distance;
              closestDungeonCell[0] = x;
              closestDungeonCell[1] = y;
            }
          }

        }
      }
      //		Console.WriteLine("Target room center coordinates are {0}:{1}.", target.CenterX, target.CenterY);
      //		Console.WriteLine("Target room left corner coordinates are {0}:{1}.", target.UpperLeftX, target.UpperLeftY);
      //		Console.WriteLine("Target room right corner coordinates are {0}:{1}.", target.LowerRightX, target.LowerRightY);
      //		Console.WriteLine("Trying to connect room with cell {0}:{1}.", closestDungeonCell[0], closestDungeonCell[1]);
      //		Console.WriteLine("Is this cell inside the room? {0}", CellInsideRoomBounds(target, closestDungeonCell[0], closestDungeonCell[1]));
      ConnectRoomWithCell(target, closestDungeonCell[0], closestDungeonCell[1]);

    }

    /// <summary>
    /// Places the doors on map and deletes fog triggers inside rooms.
    /// </summary>
    void PlaceDoorsOnMap()
    {
      foreach (DungeonRoom room in rooms)
      {
        for (int x = room.UpperLeftX - 1; x < room.LowerRightX + 1; x++)
        {
          for (int y = room.UpperLeftY - 1; y < room.LowerRightY + 1; y++)
          {
            if (x == room.UpperLeftX - 1 || x == room.LowerRightX)
            {
              switch (room.roomType)
              {
                case RoomType.KeyLockedRoom:
                  dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? KEY_LOCKED_DOOR_TILE : dungeonMap[x, y];
                  break;
                case RoomType.CollapsedRoom:
                  dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? COLLAPSED_DOOR_TILE : dungeonMap[x, y];
                  break;
                default:
                  dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? DOOR_TILE : dungeonMap[x, y];
                  break;
              }
              // dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? DOOR_TILE : dungeonMap[x, y];
            }
            else if (y == room.UpperLeftY - 1 || y == room.LowerRightY)
            {
              switch (room.roomType)
              {
                case RoomType.KeyLockedRoom:
                  dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? KEY_LOCKED_DOOR_TILE : dungeonMap[x, y];
                  break;
                case RoomType.CollapsedRoom:
                  dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? COLLAPSED_DOOR_TILE : dungeonMap[x, y];
                  break;
                default:
                  dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? DOOR_TILE : dungeonMap[x, y];
                  break;
              }
              // dungeonMap[x, y] = (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == FOG_TRIGGER_TILE) ? DOOR_TILE : dungeonMap[x, y];
            }

            //Delete unnecessary fog triggers
            else
            {
              if (dungeonMap[x, y] == FOG_TRIGGER_TILE && CellInsideRoomBounds(room, x, y))
              {
                dungeonMap[x, y] = FLOOR_TILE_0;
              }
            }
          }
        }
      }
    }

    public bool CellInsideRoomBounds(DungeonRoom room, int x, int y)
    {
      return (x >= (room.UpperLeftX - roomSurroundings) && x < (room.LowerRightX + roomSurroundings))
          && (y >= (room.UpperLeftY - roomSurroundings) && y < (room.LowerRightY + roomSurroundings));
    }

    public bool CellInsideAnyRoom(int x, int y)
    {
      foreach (DungeonRoom target in rooms)
      {
        if (CellInsideRoomBounds(target, x, y))
        {
          return true;
        }
      }
      return false;
    }

    void PlaceObjectInsideRoom(int objectType, DungeonRoom room, bool isInteractive = false)
    {
      int x = Program.rand.Next(room.UpperLeftX, room.LowerRightX);
      int y = Program.rand.Next(room.UpperLeftY, room.LowerRightY);
      int[,] map = isInteractive ? interactiveObjects : dungeonMap;
      if (room.roomType == RoomType.RadiationRoom && objectType != FLOOR_TILE_0)
      {
        for (int radX = room.UpperLeftX; radX < room.LowerRightX; radX++)
        {
          for (int radY = room.UpperLeftY; radY < room.LowerRightY; radY++)
          {
            if (dungeonMap[radX, radY] == FLOOR_TILE_0)
            {
              map[radX, radY] = objectType;
              return;
            }

          }
        }
      }
      else
      {
        if (dungeonMap[x, y] == FLOOR_TILE_0 || dungeonMap[x, y] == RADIATION_TILE)
        {
          map[x, y] = objectType;
        }
        else
        {
          PlaceObjectInsideRoom(objectType, room, isInteractive);
        }
      }
    }

    void FillDungeonWithInteractiveObjects()
    {
      List<string> dungItems = GenerateObjectsForSpecialRooms();
      foreach (var item in dungItems)
      {
        DungeonRoom targetRoom;
        switch (item)
        {
          case "RandomLoot":
            targetRoom = rooms.FirstOrDefault(x => (x.roomType == RoomType.SecretRoom && x.AdditionalInfo != "Loot")) ?? rooms.FirstOrDefault(x => x.AdditionalInfo == "None");
            if (targetRoom != null)
            {
              targetRoom.AdditionalInfo = "Loot";
              PlaceObjectInsideRoom(RANDOM_LOOT, targetRoom, true);
            }
            else
              Console.WriteLine("Couldn't find room for  " + item);
            break;
          case "Key":
            targetRoom = rooms.FirstOrDefault(x => (x.roomType != RoomType.KeyLockedRoom && x.AdditionalInfo != "Key")) ?? rooms.FirstOrDefault(x => x.AdditionalInfo == "None");
            if(targetRoom != null)
            {
              targetRoom.AdditionalInfo = "Key";
              PlaceObjectInsideRoom(KEY, targetRoom, true);
            }
            else
              Console.WriteLine("Couldn't find room for  " + item);
            break;
          case "RandomLootForKey":
            targetRoom = rooms.Where(x => (x.roomType == RoomType.KeyLockedRoom && x.AdditionalInfo != "Loot")).FirstOrDefault();
            if (targetRoom != null)
            {
              targetRoom.AdditionalInfo = "Loot";
              PlaceObjectInsideRoom(KEY_ROOM_LOOT, targetRoom, true);
            }
            else
              Console.WriteLine("Couldn't find room for  " + item);
            break;
          case "RadiationRoomLoot":
            targetRoom = rooms.Where(x => (x.roomType == RoomType.RadiationRoom && x.AdditionalInfo != "Loot")).FirstOrDefault();
            if (targetRoom != null)
            {
              targetRoom.AdditionalInfo = "Loot";
              PlaceObjectInsideRoom(RADIATION_LOOT, targetRoom, true);
            }
            else
              Console.WriteLine("Couldn't find room for  " + item);
            break;
          case "Dynamite":
            targetRoom = rooms.Where(x => (x.roomType != RoomType.CollapsedRoom && x.AdditionalInfo != "Loot")).FirstOrDefault();
            if (targetRoom != null)
            {
              targetRoom.AdditionalInfo = "Dynamite";
              PlaceObjectInsideRoom(DYNAMITE, targetRoom, true);
            }
            else
              Console.WriteLine("Couldn't find room for  " + item);
            break;
          default:
            break;
        }
      }

      for (int x = 0; x < interactiveObjects.GetLength(0); x++)
      {
        for (int y = 0; y < interactiveObjects.GetLength(1); y++)
        {
          if (interactiveObjects[x, y] != 0)
            dungeonMap[x, y] = interactiveObjects[x, y];
        }
      }
    }

    List<string> GenerateObjectsForSpecialRooms()
    {
      List<string> dungObjects = new List<string>();
      IEnumerable<DungeonRoom> specialRooms = rooms.Where(x => x.roomType != RoomType.RegularRoom);

      foreach (var room in specialRooms)
      {
        switch (room.roomType)
        {
          case RoomType.SecretRoom:
            dungObjects.Add("RandomLoot");
            break;
          case RoomType.RadiationRoom:
            dungObjects.Add("RadiationRoomLoot");
            break;
          case RoomType.KeyLockedRoom:
            dungObjects.Add("Key");
            dungObjects.Add("RandomLootForKey");
            break;
          case RoomType.CollapsedRoom:
            int chance = Program.rand.Next(100);
            if (chance > 50)
              dungObjects.Add("Dynamite");
            break;
          case RoomType.RegularRoom:
          default:
            break;
        }
      }
      return dungObjects;
    }

    void DiscoverRoom(DungeonRoom room)
    {
      for (int x = room.UpperLeftX - 1; x < room.LowerRightX + 1; x++)
      {
        for (int y = room.UpperLeftY - 1; y < room.LowerRightY + 1; y++)
        {
          fogOfWarMap[x, y] = true;
        }
      }
    }

    int[,] CreateRenderingMap(int[,] logicalMap)
    {
      int[,] renderingMap = new int[logicalMap.GetLength(0), logicalMap.GetLength(1)];
      for (int x = 0; x < logicalMap.GetLength(0); x++)
      {
        for (int y = 0; y < logicalMap.GetLength(1); y++)
        {
          switch (logicalMap[x, y])
          {
            case WALL_TILE:
            case VERTICAL_WALL_TILE:
            case HORIZONTAL_WALL_TILE:
            case SURROUNDING_WALL_TILE:
            case CORNER_TILE:
              renderingMap[x, y] = WALL_TILE;
              break;
            case FLOOR_TILE_0:
            case FOG_TRIGGER_TILE:
              renderingMap[x, y] = FLOOR_TILE_0;
              break;
            case START_TILE:
              renderingMap[x, y] = START_TILE;
              break;
            case EXIT_TILE:
              renderingMap[x, y] = EXIT_TILE;
              break;
            case DOOR_TILE:
              renderingMap[x, y] = DOOR_TILE;
              break;
            case SECRET_DOOR_TILE:
              renderingMap[x, y] = SECRET_DOOR_TILE;
              break;
            default:
              renderingMap[x, y] = logicalMap[x, y];
              break;

          }
        }
      }
      
      return renderingMap;
    }

    bool[,] CreateWalkableMap(int[,] renderingMap)
    {
      bool[,] walkableMap = new bool[renderingMap.GetLength(0), renderingMap.GetLength(1)];
      for (int x = 0; x < renderingMap.GetLength(0); x++)
      {
        for (int y = 0; y < renderingMap.GetLength(1); y++)
        {
          switch (renderingMap[x, y])
          {
            case WALL_TILE:
            case VERTICAL_WALL_TILE:
            case HORIZONTAL_WALL_TILE:
            case SURROUNDING_WALL_TILE:
            case SECRET_DOOR_TILE:
            case CORNER_TILE:
              walkableMap[x, y] = false;
              break;
            case FLOOR_TILE_0:
            case DOOR_TILE:
            case START_TILE:
            case EXIT_TILE:
            case RADIATION_TILE:
              walkableMap[x, y] = true;
              break;
            default:
              walkableMap[x, y] = false;
              break;

          }
        }
      }
      return walkableMap;
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("\n=============Start of the dungeon #"+ dungNum + "==================\n");
      sb.Append(System.String.Format("Dungeon of {0} columns and {1} rows, containing {2} rooms.\n", columns, rows, rooms.Count));
      sb.Append("Dungeon map:\n");
      for (int x = 0; x < columns; x++)
      {
        sb.Append("\n");
        for (int y = 0; y < rows; y++)
        {
          sb.Append(dungeonMap[x, y].ToString() + " ");
        }
      }
      sb.Append("\n=============End of the dungeon==================\n");
      return sb.ToString();
    }
  }

  public class DungeonRoom
  {
    public RoomType roomType { get; set; }
    public int UpperLeftX { get; set; }
    public int UpperLeftY { get; set; }
    public int LowerRightX { get; set; }
    public int LowerRightY { get; set; }
    public int CenterX { get; set; }
    public int CenterY { get; set; }
    public int RoomWidth { get; set; }
    public int RoomHeight { get; set; }

    public string AdditionalInfo { get; set; } = "None";


    public DungeonRoom(int x, int y, int width, int height)
    {
      UpperLeftX = x;
      UpperLeftY = y;
      RoomWidth = width;
      RoomHeight = height;
      LowerRightX = UpperLeftX + RoomWidth;
      LowerRightY = UpperLeftY + RoomHeight;
      CenterX = UpperLeftX + RoomWidth / 2;
      CenterY = UpperLeftY + RoomHeight / 2;
      roomType = RoomType.RegularRoom;
    }

    public DungeonRoom(int x, int y, int width, int height, RoomType rType)
    {
      UpperLeftX = x;
      UpperLeftY = y;
      RoomWidth = width;
      RoomHeight = height;
      LowerRightX = UpperLeftX + RoomWidth;
      LowerRightY = UpperLeftY + RoomHeight;
      CenterX = UpperLeftX + RoomWidth / 2;
      CenterY = UpperLeftY + RoomHeight / 2;
      roomType = rType;
    }

    public DungeonRoom(RoomType rType)
    {
      roomType = rType;
    }


    /*public bool UnitInsideRoom(Vector3 transform) 
            {
                return((transform.x >= UpperLeftX && transform.x <= LowerRightX) && (transform.y >= LowerRightY && transform.y <= UpperLeftY));
            }*/

    public bool UnitInsideRoom(int x, int y)
    {
      return ((x >= UpperLeftX && x <= LowerRightX) && (y >= UpperLeftY && y <= LowerRightY));
    }

    /*public bool UnitInsideRoom(Vector2 coords) 
            {
                return((coords.x >= UpperLeftX && coords.x <= LowerRightX) && (coords.y >= LowerRightY && coords.y <= UpperLeftY));
            }*/

    public override string ToString()
    {
      return System.String.Format("Room. Center coords: {0};{1}. Width is {2}, height is {3}.", CenterX, CenterY, RoomWidth, RoomHeight);
    }

  }

  public enum RoomType
  { RegularRoom = 0, SecretRoom = 1, RadiationRoom = 2, KeyLockedRoom = 3, CollapsedRoom = 4 }


  public class DungeonDoor
  {
    public int X { get; set; }
    public int Y { get; set; }

    public bool Locked { get; set; }

    public DungeonDoor(int x, int y)
    {
      X = x;
      Y = y;
      Locked = false;
    }

  }
}
