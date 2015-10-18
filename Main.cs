using System.IO;
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
	static void Main()
	{
		DungeonGenerator dunGen = new DungeonGenerator();
		dunGen.CreateSimpleDungeonMap(8);
		
		Console.WriteLine(dunGen.ToString());
		for (int i = 0; i < 100; i++)
		{
			dunGen = new DungeonGenerator();
			dunGen.CreateSimpleDungeonMap(8);
			using (StreamWriter sw = File.AppendText(@"dungeons2.txt"))
				sw.WriteLine(dunGen.ToString());
		}
		Console.WriteLine("Success");
		Console.ReadKey();
	}
}
public class DungeonGenerator 
{
	
	Random rand = new Random();
	
	readonly int minRoomWidth = 4;
	readonly int maxRoomWidth = 8;
	readonly int minRoomHeight = 3;
	readonly int maxRoomHeight = 7;
	
	//used in calculating dungeon's dimensions
	private const int dungeonStretchCoeff = 9;
	private const int roomSurroundings = 2;
	
	const int WALL_TILE             = 9;
	const int SURROUNDING_WALL_TILE = 6;
	const int FLOOR_TILE_0          = 0;
	const int VERTICAL_WALL_TILE    = 1;
	const int HORIZONTAL_WALL_TILE  = 2;
	const int CORNER_TILE           = 3;
	const int START_TILE            = 7;
	const int EXIT_TILE             = 8;
	const int DOOR_TILE             = 5;
	const int CHEST_TILE            = 10;
	
	int[,] dungeonMap, objectsLayer;
	int columns, rows;
	List<DungeonRoom> rooms = new List<DungeonRoom>();
	List<DungeonDoor> doors = new List<DungeonDoor>();
	
	public DungeonRoom startRoom;
	
	
	public int[,] CreateSimpleDungeonMap(int roomsAmount)
	{
		//Assess approximatie dungeon dimensions
		int approxDungeonArea = (int) (dungeonStretchCoeff * roomsAmount * ((minRoomWidth * minRoomHeight) + (maxRoomWidth * maxRoomHeight)) / 2);
		columns = (int)Math.Sqrt(approxDungeonArea);
		rows = columns;
		dungeonMap = new int[columns, rows];
		
		//fill dungeon with walls
		for (int x = 0; x < columns; x++) 
		{
			for (int y = 0; y < rows; y++)
			{
				dungeonMap[x,y] = WALL_TILE;
			}
		}
		
		//create rooms array and place them on map
		for(int i = 0; i < roomsAmount; i++)
		{
			int roomX = rand.Next(4, columns - maxRoomWidth - 4);
			int roomY = rand.Next(4, rows - maxRoomHeight - 4);
			int roomWidth = rand.Next(minRoomWidth, maxRoomWidth);
			int roomHeight = rand.Next(minRoomHeight, maxRoomHeight);
			DungeonRoom roomToAdd = new DungeonRoom(roomX, roomY, roomWidth, roomHeight);
			int iterations = 0;
			
			while (!AbleToPlace(roomToAdd))
			{
				roomX = rand.Next(4, columns - maxRoomWidth - 4);
				roomY = rand.Next(4, rows - maxRoomHeight - 4);
				roomToAdd = new DungeonRoom(roomX, roomY, roomWidth, roomHeight);
				iterations++;
				if(iterations > 14000) return dungeonMap;
			}
			rooms.Add(roomToAdd);
			PlaceRoomOnMap(roomToAdd);
		}
		//Connect all rooms
		//ConnectRoomsThroughCenter();
		/*for(int k = 0; k < rooms.Count - 1; k++)
                {
                    ConnectRoomsV2(rooms[k], rooms[k + 1]);
                }*/
		startRoom = rooms[rand.Next(0, rooms.Count - 1)];
		return dungeonMap;
		
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
	
	void PlaceRoomOnMap(DungeonRoom room) 
	{
		for (int x = room.UpperLeftX - roomSurroundings; x < room.LowerRightX + roomSurroundings; x++)
		{
			for (int y = room.UpperLeftY - roomSurroundings; y < room.LowerRightY + roomSurroundings; y++)
			{
				if(x == room.UpperLeftX - roomSurroundings || x == room.LowerRightX + (roomSurroundings -1))
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
					dungeonMap[x, y] = FLOOR_TILE_0;
				}
				dungeonMap[room.UpperLeftX - 1, room.UpperLeftY - 1] = CORNER_TILE;
				dungeonMap[room.UpperLeftX - 1, room.LowerRightY] = CORNER_TILE;
				dungeonMap[room.LowerRightX, room.UpperLeftY - 1] = CORNER_TILE;
				dungeonMap[room.LowerRightX, room.LowerRightY] = CORNER_TILE;
			} 
		}
		AddRoomToDungeon(room);
	}
	
	DungeonRoom FindNearestRoom(DungeonRoom target)
	{
		if(rooms.Count <= 1) 
		{
			return null;
		}
		else
		{
			int targetX = target.CenterX;
			int targetY = target.CenterY;
			int nearestDistance = 0;
			DungeonRoom nearestRoom = null;
			foreach(DungeonRoom room in rooms)
			{
				int distance = (int)(Math.Abs(room.CenterX - target.CenterX) + Math.Abs(room.CenterY - target.CenterY));
				if(nearestDistance != 0 && (room.CenterX != targetX || room.CenterY !=targetY))
				{
					if (distance < nearestDistance)
					{
						nearestDistance = distance;
						nearestRoom = room;
					}
				}
				else if(nearestDistance == 0 && (room.CenterX != targetX || room.CenterY != targetY))
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
			if(!roomFound)
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
	
	void ConnectRooms(DungeonRoom room1, DungeonRoom room2)
	{
		int x1 = room1.CenterX;
		int y1 = room1.CenterY;
		int x2 = room2.CenterX;
		int y2 = room2.CenterY;
		
		int xDir = (x2 > x1) ? 1 : -1;
		int yDir = (y2 > y1) ? 1 : -1;
		
		//horizontal corridor
		for (int x = x1; x != x2; x += xDir)
		{
			if (dungeonMap[x, y1] == SURROUNDING_WALL_TILE) {
				if((dungeonMap[x, y1 + 1] == SURROUNDING_WALL_TILE || dungeonMap[x, y1 + 1] == DOOR_TILE) 
				   && (dungeonMap[x, y1 - 1] == SURROUNDING_WALL_TILE || dungeonMap[x, y1 - 1] == DOOR_TILE)
				   && dungeonMap[x + 1, y1] != SURROUNDING_WALL_TILE)
				{
					dungeonMap[x, y1] = DOOR_TILE;
					doors.Add(new DungeonDoor(x, y1));
				}
				else
				{
					dungeonMap[x, y1] = FLOOR_TILE_0;
				}
			}
			else
			{
				dungeonMap[x, y1] = FLOOR_TILE_0;
			}
		}
		
		//vertical corridor
		for (int y = y1; y != y2; y += yDir)
		{
			if(dungeonMap[x2, y] == SURROUNDING_WALL_TILE) 
			{
				if ((dungeonMap[x2 + 1, y] == SURROUNDING_WALL_TILE || dungeonMap[x2 + 1, y] == DOOR_TILE) &&
				    (dungeonMap[x2 - 1, y] == SURROUNDING_WALL_TILE || dungeonMap[x2 - 1, y] == DOOR_TILE))
				{
					dungeonMap[x2, y] = DOOR_TILE;
					doors.Add(new DungeonDoor(x2, y));
				}
				else
				{
					dungeonMap[x2, y] = FLOOR_TILE_0; 
				}
			}
			else
			{
				dungeonMap[x2, y] = FLOOR_TILE_0;
			}
		}
	}
	
	void ConnectRoomsV2(DungeonRoom room1, DungeonRoom room2)
	{
		int x1 = room1.CenterX;
		int y1 = room1.CenterY;
		int x2 = room2.CenterX;
		int y2 = room2.CenterY;
		
		int xDir = (x2 > x1) ? 1 : -1;
		int yDir = (y2 > y1) ? 1 : -1;
		
		//horizontal corridor
		for (int x = x1; x != x2; x += xDir)
		{
			if (dungeonMap[x, y1] == VERTICAL_WALL_TILE) 
			{
				dungeonMap[x, y1] = DOOR_TILE;
				doors.Add(new DungeonDoor(x, y1));
			}
			else if(dungeonMap[x, y1] == CORNER_TILE || dungeonMap[x, y1] == HORIZONTAL_WALL_TILE)
			{
				y1 += yDir;
				switch(dungeonMap[x, y1])
				{
				case VERTICAL_WALL_TILE:
					dungeonMap[x - xDir, y1] = FLOOR_TILE_0;
					dungeonMap[x, y1] = DOOR_TILE;
					break;
				default:
					dungeonMap[x - xDir, y1] = FLOOR_TILE_0;
					dungeonMap[x, y1] = FLOOR_TILE_0;
					break;
				}
			}
			else
			{
				dungeonMap[x, y1] = FLOOR_TILE_0;
			}
		}
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
			if (dungeonMap[x, y1] == VERTICAL_WALL_TILE) 
			{
				dungeonMap[x, y1] = DOOR_TILE;
				doors.Add(new DungeonDoor(x, y1));
			}
			else if(dungeonMap[x, y1] == CORNER_TILE || dungeonMap[x, y1] == HORIZONTAL_WALL_TILE)
			{
				y1 += yDir;
				switch(dungeonMap[x, y1])
				{
				case VERTICAL_WALL_TILE:
					dungeonMap[x - xDir, y1] = FLOOR_TILE_0;
					dungeonMap[x, y1] = DOOR_TILE;
					break;
				default:
					dungeonMap[x - xDir, y1] = FLOOR_TILE_0;
					dungeonMap[x, y1] = FLOOR_TILE_0;
					break;
				}
			}
			else
			{
				dungeonMap[x, y1] = FLOOR_TILE_0;
			}
		}
		
		//vertical corridor
		for (int y = y1; y != y2 + yDir; y += yDir)
		{
			if(dungeonMap[x2, y] == HORIZONTAL_WALL_TILE) 
			{
				dungeonMap[x2, y] = DOOR_TILE;
				doors.Add(new DungeonDoor(x2, y));
			}
			else if(dungeonMap[x2, y] == CORNER_TILE || dungeonMap[x2, y] == VERTICAL_WALL_TILE)
			{
				x2 += xDir;
				switch(dungeonMap[x2, y])
				{
				case VERTICAL_WALL_TILE:
					dungeonMap[x2, y - yDir] = FLOOR_TILE_0;
					dungeonMap[x2, y] = DOOR_TILE;
					break;
				default:
					dungeonMap[x2, y - yDir] = FLOOR_TILE_0;
					dungeonMap[x2, y] = FLOOR_TILE_0;
					break;
				}
			}
			else
			{
				dungeonMap[x2, y] = FLOOR_TILE_0;
			}
		}
	}
	
	void AddRoomToDungeon(DungeonRoom target)
	{
		if(rooms.Count < 2) return;
		int[] closestDungeonCell = {9999, 9999};
		int closestDistance = Math.Abs(closestDungeonCell[0] - target.CenterX) + Math.Abs(closestDungeonCell[1] - target.CenterY);
		for (int x = 0; x < columns; x++)
		{
			for (int y = 0; y < rows; y++)
			{
				if(CellInsideRoomBounds(target, x, y) || (dungeonMap[x, y] != FLOOR_TILE_0 && dungeonMap[x, y] != DOOR_TILE)) 
				{
					continue; 
				}
				else
				{
					int distance = Math.Abs(x - target.CenterX) + Math.Abs(y - target.CenterY);
					if (distance <= closestDistance)
					{
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
	
	public void ConnectRoomsThroughCenter()
	{
		DungeonRoom closestToCenter = FindClosestToCenterRoom();
		
		foreach (DungeonRoom room in rooms) 
		{
			ConnectRooms(closestToCenter, room);
		}
		
	}
	
	public void RemoveExcessDoors()
	{
		foreach(DungeonDoor door in doors)
		{
			int doorX = door.X;
			int doorY = door.Y;
			if ((dungeonMap[doorX - 1, doorY] == FLOOR_TILE_0 || dungeonMap[doorX + 1, doorY] == FLOOR_TILE_0)
			    &&(dungeonMap[doorX, doorY - 1] == FLOOR_TILE_0 || dungeonMap[doorX, doorY + 1] == FLOOR_TILE_0))
			{
				dungeonMap[doorX, doorY] =  FLOOR_TILE_0;
			}
		}
	}
	
	public bool CellInsideRoomBounds(DungeonRoom room, int x, int y) 
	{
		return(x >= (room.UpperLeftX - roomSurroundings) && x < (room.LowerRightX + roomSurroundings)) 
			&& (y >= (room.UpperLeftY - roomSurroundings) && y < (room.LowerRightY + roomSurroundings));
	}
	
	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("\n=============Start of the dungeon==================\n");
		sb.Append(System.String.Format("Dungeon of {0} columns and {1} rows, containing {2} rooms.\n", columns, rows, rooms.Count));
		sb.Append("Dungeon map:\n");
		for (int x = 0; x < columns; x++) 
		{
			sb.Append("\n");
			for (int y = 0; y < rows; y++)
			{
				sb.Append(dungeonMap[x,y].ToString() + " ");
			}
		}
		sb.Append("\n=============End of the dungeon==================\n");
		return sb.ToString();
	}
}
public class DungeonRoom 
{
	public int UpperLeftX  { get; set; }
	public int UpperLeftY  { get; set; }
	public int LowerRightX { get; set; }
	public int LowerRightY { get; set; }
	public int CenterX     { get; set; }
	public int CenterY     { get; set; }
	public int RoomWidth   { get; set; }
	public int RoomHeight  { get; set; }
	
	
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
	}
	
	/*public bool UnitInsideRoom(Vector3 transform) 
            {
                return((transform.x >= UpperLeftX && transform.x <= LowerRightX) && (transform.y >= LowerRightY && transform.y <= UpperLeftY));
            }*/
	
	public bool UnitInsideRoom(int x, int y) 
	{
		return((x >= UpperLeftX && x <= LowerRightX) && (y >= UpperLeftY && y <= LowerRightY));
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
