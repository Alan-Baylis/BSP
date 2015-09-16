using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BSP
{
    public class BSPNode
    {
        public int XPos;
        public int YPos;
        public int MapXPos;
        public int MapYPos;
        public int XSize;
        public int YSize;
        public int Depth;
        public int[,] Grid;
        public bool Active;
        public List<BSPNode> Children = new List<BSPNode>();

        // Plot grid by walking tree
        public void PlotGrid()
        {
            Grid = new int[XSize, YSize];
            if (!Active) return;
            for (int x = 0; x < XSize; x++)
                for (int y = 0; y < YSize; y++)
                {
                    if (x == 0 || y == 0 || x == XSize - 1 || y == YSize - 1) Grid[x, y] = 2;
                    else Grid[x, y] = 1;

                }
            foreach (BSPNode child in Children)
            {
                child.PlotGrid();
                for(int x=0;x<child.XSize;x++)
                    for (int y = 0; y < child.YSize; y++)
                    {
                        if (Grid[child.XPos + x, child.YPos + y] == 2) continue;
                        Grid[child.XPos + x, child.YPos + y] = child.Grid[x, y];
                    }
            }
        }

        public override string ToString()
        {
            string map = "";

            for (int y = 0; y < YSize; y++)
            {
                for (int x = 0; x < XSize; x++)
                {
                    map += Grid[x, y];
                }
                map += "\n";
            }

            map = map.Replace("0", " ");
            map = map.Replace("1", ".");

            return map;
        }
    }

    public class Room
    {
        public int XPos;
        public int YPos;
        public int XSize;
        public int YSize;

        public List<Door> Doors = new List<Door>(); 
    }

    public class Door
    {
        public Room AdjoiningRoom;
        public int XPos;
        public int YPos;
        public bool Vertical;
    }

    class Program
    {
        private const int MIN_ROOMS = 8;
        private const int MAX_ROOMS = 15;

        private const int MAX_DOOR_ATTEMPTS = 10;

        private const int MIN_SPLITSIZE = 5;
        private const int MIN_ROOMSIZE = 7;
        private static Random rand;

        static void Main(string[] args)
        {
            //int seed = 0;
            //if (args.Length == 1) seed = Convert.ToInt32(args[0]);
            rand = new Random();
           

            BSPNode map = new BSPNode()
            {
                XPos = 0,
                YPos = 0,
                MapXPos = 0,
                MapYPos = 0,
                XSize = 50,
                YSize = 50,
                Depth = 0,
                Active = true
            };

            List<Room> rooms = new List<Room>();
            Generate(map, rooms);
            map.PlotGrid();
            //Console.WriteLine(map.ToString() + "\n");

            // intersect test
            for (int i = rooms.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = rooms.Count - 1; j >= 0; j--)
                {
                    if (i == j) continue;
                    if (Intersects(rooms[i].XPos, rooms[i].YPos, rooms[i].XSize, rooms[i].YSize, rooms[j].XPos, rooms[j].YPos, rooms[j].XSize, rooms[j].YSize))
                        found = true;
                }
                if (!found) rooms.RemoveAt(i);
            }

            // Room walk test
            for (int i = rooms.Count - 1; i >= 0; i--)
            {
                List<int> breadcrumbs = new List<int>();
                breadcrumbs.Add(i);
                int walkCount = 1;
                WalkRoomsIntersect(rooms, i, breadcrumbs, ref walkCount);
                if (walkCount < rooms.Count)
                    rooms.RemoveAt(i);
            }

            // Create Doors
            for (int i = rooms.Count - 1; i >= 0; i--)
            {
                bool doorCreated = false;
                var targetRooms = (from item in rooms
                                     orderby rand.Next()
                                     select item).ToList();
                
                for (int j = targetRooms.Count - 1; j >= 0; j--)
                {
                    if (i == j) continue;
                    if (rooms[i].Doors.FirstOrDefault(dr => dr.AdjoiningRoom == targetRooms[j]) != null) continue;
                    if (Intersects(rooms[i].XPos, rooms[i].YPos, rooms[i].XSize, rooms[i].YSize, targetRooms[j].XPos,
                        targetRooms[j].YPos, targetRooms[j].XSize, targetRooms[j].YSize))
                    {
                        for (int attempts = 0; attempts < MAX_DOOR_ATTEMPTS; attempts++)
                        {
                            // Room to the left
                            if (targetRooms[j].XPos < rooms[i].XPos)
                            {
                                int dx = rooms[i].XPos;
                                int dy = rooms[i].YPos + rand.Next(rooms[i].YSize - 3) + 1;
                                if (dy > targetRooms[j].YPos && dy < targetRooms[j].YPos + (targetRooms[j].YSize - 2))
                                {
                                    doorCreated = true;
                                    rooms[i].Doors.Add(new Door() { AdjoiningRoom= targetRooms[j], Vertical=true, XPos  = dx, YPos = dy});
                                    targetRooms[j].Doors.Add(new Door() { AdjoiningRoom = rooms[i], Vertical = true, XPos = dx, YPos = dy });
                                    break;
                                }
                            }

                            // Room to the right
                            if (targetRooms[j].XPos > rooms[i].XPos)
                            {
                                int dx = targetRooms[j].XPos;
                                int dy = rooms[i].YPos + rand.Next(rooms[i].YSize - 3) + 1;
                                if (dy > targetRooms[j].YPos && dy < targetRooms[j].YPos + (targetRooms[j].YSize - 2))
                                {
                                    doorCreated = true;
                                    rooms[i].Doors.Add(new Door() { AdjoiningRoom = targetRooms[j], Vertical = true, XPos = dx, YPos = dy });
                                    targetRooms[j].Doors.Add(new Door() { AdjoiningRoom = rooms[i], Vertical = true, XPos = dx, YPos = dy });
                                    break;
                                }
                            }

                            // Room above
                            if (targetRooms[j].YPos < rooms[i].YPos)
                            {
                                int dy = rooms[i].YPos;
                                int dx = rooms[i].XPos + rand.Next(rooms[i].XSize - 3) + 1;
                                if (dx > targetRooms[j].XPos && dx < targetRooms[j].XPos + (targetRooms[j].XSize - 2))
                                {
                                    doorCreated = true;
                                    rooms[i].Doors.Add(new Door() { AdjoiningRoom = targetRooms[j], Vertical = false, XPos = dx, YPos = dy });
                                    targetRooms[j].Doors.Add(new Door() { AdjoiningRoom = rooms[i], Vertical = false, XPos = dx, YPos = dy });
                                    break;
                                }
                            }

                            // Room below
                            if (targetRooms[j].YPos > rooms[i].YPos)
                            {
                                int dy = targetRooms[j].YPos;
                                int dx = rooms[i].XPos + rand.Next(rooms[i].XSize - 3) + 1;
                                if (dx > targetRooms[j].XPos && dx < targetRooms[j].XPos + (targetRooms[j].XSize - 2))
                                {
                                    doorCreated = true;
                                    rooms[i].Doors.Add(new Door() { AdjoiningRoom = targetRooms[j], Vertical = false, XPos = dx, YPos = dy });
                                    targetRooms[j].Doors.Add(new Door() { AdjoiningRoom = rooms[i], Vertical = false, XPos = dx, YPos = dy });
                                    break;
                                }
                            }
                        }
                    }
                    if (doorCreated) break;
                }

                if(!doorCreated) rooms.RemoveAt(i);
            }

           

            // Room walk test using doors
            for (int i = rooms.Count - 1; i >= 0; i--)
            {
                List<int> breadcrumbs = new List<int>();
                breadcrumbs.Add(i);
                int walkCount = 1;
                WalkRoomsDoors(rooms, i, breadcrumbs, ref walkCount);
                if (walkCount < rooms.Count)
                {
                    for (int d = rooms[i].Doors.Count-1; d >= 0; d--)
                    {
                        rooms[i].Doors[d].AdjoiningRoom.Doors.Remove(rooms[i].Doors[d]);
                        rooms[i].Doors.RemoveAt(d);
                    }
                    rooms.RemoveAt(i);
                }
            }

            // Test to see if doors still have valid connecting rooms
            foreach (Room r in rooms)
            {
                for (int i = r.Doors.Count - 1; i >= 0; i--)
                {
                    if (r.Doors[i].AdjoiningRoom == null) r.Doors.RemoveAt(i);
                }
            }

            int[,] mapGrid = new int[map.XSize,map.YSize];
            foreach (Room r in rooms)
            {
                for(int x=0;x<r.XSize;x++)
                    for (int y = 0; y < r.YSize; y++)
                    {
                        if (x == 0 || y == 0 || x == r.XSize - 1 || y == r.YSize - 1) mapGrid[r.XPos + x, r.YPos + y] = 2;
                        else mapGrid[r.XPos + x, r.YPos + y] = 1;
                    }

                foreach (Door d in r.Doors)
                {
                    if (d.AdjoiningRoom.Doors.Count == 0) continue;
                    mapGrid[d.XPos, d.YPos] = 1;
                    if(d.Vertical)
                        mapGrid[d.XPos, d.YPos+1] = 1;
                    else
                        mapGrid[d.XPos+1, d.YPos] = 1;
                }
            }

            

            string mapOut = "    ";

            for (int y = 0; y < map.YSize; y++)
            {
                for (int x = 0; x < map.XSize; x++)
                {
                    mapOut += mapGrid[x, y];
                }
                mapOut += "\n    ";
            }

            mapOut = mapOut.Replace("0", " ");
            mapOut = mapOut.Replace("1", ".");
            Console.WriteLine(mapOut);

            if(rooms.Count<MIN_ROOMS || rooms.Count>MAX_ROOMS) Main(new[]{""});

            string s = Console.ReadLine();
            if(!string.IsNullOrEmpty(s)) Main(new []{s});
        }

        static void WalkRoomsIntersect(List<Room> rooms, int prevroom, List<int> trail, ref int count)
        {
            for (int j = rooms.Count - 1; j >= 0; j--)
            {
                if (j == prevroom) continue;
                if (trail.Contains(j)) continue;
                if (Intersects(rooms[j].XPos, rooms[j].YPos, rooms[j].XSize, rooms[j].YSize, rooms[prevroom].XPos,
                    rooms[prevroom].YPos, rooms[prevroom].XSize, rooms[prevroom].YSize))
                {
                    count++;
                    trail.Add(j);
                    WalkRoomsIntersect(rooms,j,trail,ref count);
                }

            }
        }

        static void WalkRoomsDoors(List<Room> rooms, int prevroom, List<int> trail, ref int count)
        {
            for (int j = rooms.Count - 1; j >= 0; j--)
            {
                if (j == prevroom) continue;
                if (trail.Contains(j)) continue;
                if (rooms[prevroom].Doors.FirstOrDefault(dr=>dr.AdjoiningRoom==rooms[j])!=null)
                {
                    count++;
                    trail.Add(j);
                    WalkRoomsDoors(rooms, j, trail, ref count);
                }

            }
        }

        static bool Intersects(int x1, int y1, int xs1, int ys1, int x2, int y2, int xs2, int ys2)
        {
            return !(x1 + xs1 <= x2 || x2 + xs2 <= x1 || y1 + ys1 <= y2 || y2 + ys2 <= y1);
        }

        static void Generate(BSPNode node, List<Room> rooms)
        {
            
            //if (node.Depth == MAX_DEPTH) return;

            // Split horizontally or vertically depending on size ratio or random if XSize==YSize
            int split = node.XSize > node.YSize ? 1 : node.YSize > node.XSize ? 0 : rand.Next(2);
            switch (rand.Next(2))
            {
                case 0:
                    // Vertical split
                    int splitY = (node.YSize/3) + rand.Next(node.YSize/2);
                    if (splitY < MIN_SPLITSIZE || node.YSize - splitY < MIN_SPLITSIZE)
                    {
                        rooms.Add(new Room() { XPos = node.MapXPos, YPos = node.MapYPos, XSize = node.XSize, YSize = node.YSize });
                        return;
                    }
                    BSPNode top = new BSPNode()
                    {
                        XPos = 0,
                        YPos = 0,
                        MapXPos = node.MapXPos,
                        MapYPos = node.MapYPos,
                        XSize = node.XSize,
                        YSize = splitY,
                        Depth = node.Depth + 1,
                        Active = true
                    };
                    node.Children.Add(top);
                    if (top.YSize < MIN_ROOMSIZE)
                    {
                        top.Active = false;

                    }
                    else
                    {
                        

                        Generate(top, rooms);
                    }
                    BSPNode bottom = new BSPNode()
                    {
                        XPos = 0,
                        YPos = splitY-1,
                        MapXPos = node.MapXPos,
                        MapYPos = node.MapYPos + (splitY-1),
                        XSize = node.XSize,
                        YSize = (node.YSize - splitY) +1,
                        Depth = node.Depth + 1,
                        Active = true

                    };
                    node.Children.Add(bottom);
                    if (bottom.YSize < MIN_ROOMSIZE)
                    {
                        bottom.Active = false;

                    }
                    else
                    {
               
                        Generate(bottom, rooms);
                    }
                    break;
                case 1:
                    // Horizontal split
                    int splitX = (node.XSize / 3) + rand.Next(node.XSize / 2);
                    if (splitX < MIN_SPLITSIZE || node.XSize - splitX < MIN_SPLITSIZE)
                    {
                        rooms.Add(new Room() { XPos = node.MapXPos, YPos = node.MapYPos, XSize = node.XSize, YSize = node.YSize });
                        return;
                    }
                    BSPNode left = new BSPNode()
                    {
                        XPos = 0,
                        YPos = 0,
                        MapXPos = node.MapXPos,
                        MapYPos = node.MapYPos,
                        XSize = splitX,
                        YSize = node.YSize,
                        Depth = node.Depth + 1,
                        Active = true

                    };
                    node.Children.Add(left);
                    if (left.XSize < MIN_ROOMSIZE)
                    {
                        left.Active = false;
                    }
                    else
                    {
                

                        Generate(left, rooms);
                    }
                     
                    BSPNode right = new BSPNode()
                    {
                        XPos = splitX-1,
                        YPos = 0,
                        MapXPos = node.MapXPos + (splitX-1),
                        MapYPos = node.MapYPos,
                        XSize = (node.XSize - splitX) + 1,
                        YSize = node.YSize,
                        Depth = node.Depth + 1,
                        Active = true

                    };
                    node.Children.Add(right);
                    if (right.XSize < MIN_ROOMSIZE)
                    {
                        right.Active = false;

                    }
                    else
                    {
    

                        Generate(right, rooms);
                    }
                    break;
            }

            

        }
    }
}
