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
    }

    class Program
    {
        private const int MIN_SPLITSIZE = 5;
        private const int MIN_ROOMSIZE = 7;
        private static Random rand;

        static void Main(string[] args)
        {
            int seed = 0;
            if (args.Length == 1) seed = Convert.ToInt32(args[0]);
            rand = new Random(seed);

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
            Console.WriteLine(map.ToString() + "\n");

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

            int[,] mapGrid = new int[map.XSize,map.YSize];
            foreach (Room r in rooms)
            {
                for(int x=0;x<r.XSize;x++)
                    for (int y = 0; y < r.YSize; y++)
                    {
                        if (x == 0 || y == 0 || x == r.XSize - 1 || y == r.YSize - 1) mapGrid[r.XPos + x, r.YPos + y] = 2;
                        else mapGrid[r.XPos + x, r.YPos + y] = 1;
                    }
            }

            string mapOut = "";

            for (int y = 0; y < map.YSize; y++)
            {
                for (int x = 0; x < map.XSize; x++)
                {
                    mapOut += mapGrid[x, y];
                }
                mapOut += "\n";
            }

            mapOut = mapOut.Replace("0", " ");
            mapOut = mapOut.Replace("1", ".");
            Console.WriteLine(mapOut);

            string s = Console.ReadLine();
            if(!string.IsNullOrEmpty(s)) Main(new []{s});
        }

        static bool Intersects(int x1, int y1, int xs1, int ys1, int x2, int y2, int xs2, int ys2)
        {
            return !(x1 + xs1 < x2 || x2 + xs2 < x1 || y1 + ys1 < y2 || y2 + ys2 < y1);
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
