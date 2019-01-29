using SpatialPartitioningTest01;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Vectors;

namespace SpatialPartitioningTest01
{
    public class SpaceObject
    {
        public Vector3 position; // encapsulate in a 3d object - modifiable
        public SpaceObject prev, next;

        public void updatePosition()
        {

        }
        public void RandomizePosition(Random rand, float rangeMin, float rangeMax)
        {
            float posX = rand.Next((int)(rangeMax - rangeMin)) + rangeMin;
            float posZ = rand.Next((int)(rangeMax - rangeMin)) + rangeMin;
            position.x = posX;
            position.z = posZ;
        }
    }
    public class Asteroid
    {
        public SpaceObject spaceObject;
        public Asteroid()
        {
            spaceObject = new SpaceObject();
        }
    }
    class Program
    {
        
        static void Main(string[] args)
        {
            int numAsteroids = 1000;
            float range = 10000;
            Asteroid[] ast = new Asteroid[numAsteroids];
            Random rand = new Random(100);

            SpatialPartitionPattern.Grid partition = new SpatialPartitionPattern.Grid(range, 500);

            for (int i = 0; i < numAsteroids; i++)
            {
                ast[i] = new Asteroid();
                ast[i].spaceObject.RandomizePosition(rand, partition.RangeMin, partition.RangeMax);
                partition.Add(ast[i].spaceObject);
            }

            partition.PrintCountPerCell();

            int successCount = 0;
            int numTestRuns = 50;
            for (int testCount = 0; testCount < numTestRuns; testCount++)
            {
                Vector3 center = new Vector3(rand.Next(-4000, 4000), 0, rand.Next(-4000, 4000));
                int dist = rand.Next(400, 1800);
                Console.WriteLine("Running capture test.. iteration {0}, x: {1}, z: {2}, dist: {3}", testCount, (int)center.x, (int)center.z, dist);
                if (RunCaptureTest((int) center.x, (int) center.z, dist, ast, partition) == false)
                {
                    Console.WriteLine("**** failed **** iteration {0}, x: {1}, z: {2}, dist: {3}", testCount, (int)center.x, (int)center.z, dist);
                }
                else
                {
                    successCount++;
                }

                Console.WriteLine("Num successes: {0} out of {1} test runs", successCount, numTestRuns);
            }
           /* List<SpaceObject> myList = partition.GetAll((int)center.x, (int)center.z, dist);

            Console.WriteLine("printing close asteroids");
            Console.WriteLine("num close by partition is: {0}", myList.Count);

            foreach (var t in myList)
            {
                if (Vector3.DistanceSquared(center, t.position) < dist * dist)
                {
                    Console.WriteLine("valid vector");
                }
                else
                {
                    Console.WriteLine("**** invalid vector ****");
                }
            }

            
            int countAsteroids = GetNumAsteroidsClose(ast, (int)center.x, (int)center.z, dist);
            Console.WriteLine("num close by dist is: {0}", countAsteroids);*/


            Console.WriteLine("Hello World!");
        }
        static int GetNumAsteroidsClose (Asteroid[] ast, int searchX, int searchZ, int range)
        {
            int count = 0;
            Vector3 center = new Vector3(searchX, 0, searchZ);
            int dist = range* range;

            foreach (var t in ast)
            {
                if (Vector3.DistanceSquared(center, t.spaceObject.position) < dist)
                {
                    count++;
                }
            }
            return count;
        }
        static bool RunCaptureTest(int centerX, int centerZ, int range, Asteroid[] ast, SpatialPartitionPattern.Grid partition)
        {
            Vector3 center = new Vector3(-100, 0, -100);
            int dist = 1200;
            List<SpaceObject> myList = partition.GetAll((int)center.x, (int)center.z, dist);

           /* Console.WriteLine("printing close asteroids");
            Console.WriteLine("num close by partition is: {0}", myList.Count);*/

            foreach (var t in myList)
            {
                if (Vector3.DistanceSquared(center, t.position) < dist * dist)
                {
                 //   Console.WriteLine("valid vector");
                }
                else
                {
                 //   Console.WriteLine("**** invalid vector ****");
                }
            }


            int countAsteroids = GetNumAsteroidsClose(ast, (int)center.x, (int)center.z, dist);
           // Console.WriteLine("num close by dist is: {0}", countAsteroids);


            if (countAsteroids == myList.Count)
                return true;
            return false;
        }
        static bool RunDeleteTest1(int centerX, int centerZ, int range, Asteroid[] ast, SpatialPartitionPattern.Grid partition)
        {
            partition.ClearAll();
            partition.Add(ast[0].spaceObject);

            Vector3 center = new Vector3(-100, 0, -100);
            int dist = 1200;
            List<SpaceObject> myList = partition.GetAll((int)center.x, (int)center.z, dist);

        /*    foreach (var t in myList)
            {
                if (Vector3.DistanceSquared(center, t.position) < dist * dist)
                {
                    //   Console.WriteLine("valid vector");
                }
                else
                {
                    //   Console.WriteLine("**** invalid vector ****");
                }
            }


            int countAsteroids = GetNumAsteroidsClose(ast, (int)center.x, (int)center.z, dist);*/
            // Console.WriteLine("num close by dist is: {0}", countAsteroids);


            if (countAsteroids == myList.Count)
                return true;
            return false;
        }
    }

    

}


namespace SpatialPartitionPattern
{
    public class Grid
    {
        //Need this to convert from world coordinate position to cell position
        int cellSize;
        int rangeMin, rangeMax;
        int numberOfCells;

        //This is the actual grid, where a SpaceObject is in each cell
        //Each individual SpaceObject links to other SpaceObjects in the same cell
        SpaceObject[,] cells;
        List<SpaceObject> recentlyAddedObjects;
        List<SpaceObject> recentlyMovedObjects;
        List<SpaceObject> recentlyDeletedObjects;

        public int RangeMin
        {
            get { return rangeMin; }
        }
        public int RangeMax
        {
            get { return rangeMax; }
        }

        //Init the grid
        public Grid(float mapWidth, int cellSize)
        {
            this.cellSize = cellSize;
            this.rangeMin = (int)-mapWidth;
            this.rangeMax = (int)mapWidth;
            /*mapWidth += cellSize;// need to allow off-by-one
            mapWidth *= 2;*/

            numberOfCells = (int)mapWidth / cellSize;

            cells = new SpaceObject[numberOfCells, numberOfCells];
            recentlyAddedObjects = new List<SpaceObject>();
            recentlyMovedObjects = new List<SpaceObject>();
            recentlyDeletedObjects = new List<SpaceObject>();
        }

        public void ClearAll()
        {
            for(int z=0; z< numberOfCells; z++ )
            {
                for(int x=0; x< numberOfCells; x++)
                {
                    cells[x, z] = null;
                }
            }
            recentlyAddedObjects.Clear();
            recentlyMovedObjects.Clear();
            recentlyDeletedObjects.Clear();
        }
        
        public void Add(SpaceObject spaceObject)
        {
            //Determine which grid cell the SpaceObject is in
            int cellX = NormalizeCell(spaceObject.position.x);
            int cellZ = NormalizeCell(spaceObject.position.z);

            //Add the soldier to the front of the list for the cell it's in
            spaceObject.prev = null;
            spaceObject.next = cells[cellX, cellZ];
            
            cells[cellX, cellZ] = spaceObject;

            if (spaceObject.next != null)
            {
                //Set this soldier to be the previous soldier of the next soldier of this soldier (linked lists ftw)
                spaceObject.next.prev = spaceObject;
            }
            recentlyAddedObjects.Add(spaceObject);
        }

        public void Remove(SpaceObject spaceObject)
        {
            //Determine which grid cell the SpaceObject is in
            int cellX = NormalizeCell(spaceObject.position.x);
            int cellZ = NormalizeCell(spaceObject.position.z);

            SpaceObject temp = cells[cellX, cellZ];

            while (temp != null)
            {
                if(temp == spaceObject)
                {
                    recentlyDeletedObjects.Add(spaceObject);
                    if (temp.prev != null)
                    {
                        temp.prev.next = temp.next;
                        if (temp.next != null)
                        {
                            temp.next.prev = temp.prev;
                        }
                    }
                    else
                    {
                        cells[cellX, cellZ] = temp.next;
                        temp.next.prev = null;
                    }
                    break;
                }
                temp = temp.next;
            }           
        }

        int NormalizeCell(float pos)
        {
            int floor = ((int)pos - rangeMin);
            //floor += Math.Sign(floor) * (cellSize >> 1);// rounding off 
            int cell = floor / cellSize;
            return cell;
        }
        //Get the closest enemy from the grid
        public SpaceObject FindClosestEnemy(SpaceObject spaceObject)
        {
            //Determine which grid cell the friendly soldier is in
            int cellX = NormalizeCell(spaceObject.position.x);
            int cellZ = NormalizeCell(spaceObject.position.z);

            //Get the first enemy in grid
            SpaceObject enemy = cells[cellX, cellZ];

            //Find the closest soldier of all in the linked list
            SpaceObject closestSpaceObject = null;

            float bestDistSqr = (float) double.PositiveInfinity;

            //Loop through the linked list
            while (enemy != null)
            {
                //The distance sqr between the soldier and this enemy
                float distSqr = Vector3.DistanceSquared(enemy.position, spaceObject.position);

                //If this distance is better than the previous best distance, then we have found an enemy that's closer
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;

                    closestSpaceObject = enemy;
                }

                //Get the next enemy in the list
                enemy = enemy.next;
            }

            return closestSpaceObject;
        }


        //A soldier in the grid has moved, so see if we need to update in which grid the soldier is
        public void Move(SpaceObject spaceObject, Vector3 oldPos)
        {
            //See which cell it was in 
            int oldCellX = NormalizeCell(oldPos.x);
            int oldCellZ = NormalizeCell(oldPos.z);

            //See which cell it is in now
            int cellX = NormalizeCell(spaceObject.position.x);
            int cellZ = NormalizeCell(spaceObject.position.z);

            //If it didn't change cell, we are done
            if (oldCellX == cellX && oldCellZ == cellZ)
            {
                return;
            }

            //Unlink it from the list of its old cell
            if (spaceObject.prev != null)
            {
                spaceObject.prev.next = spaceObject.next;
            }

            if (spaceObject.next != null)
            {
                spaceObject.next.prev = spaceObject.prev;
            }

            //If it's the head of a list, remove it
            if (cells[oldCellX, oldCellZ] == spaceObject)
            {
                cells[oldCellX, oldCellZ] = spaceObject.next;
            }

            //Add it bacl to the grid at its new cell
            Add(spaceObject);
            recentlyMovedObjects.Add(spaceObject);
        }

        private int AssignAndRangeCheck(int p, int range)
        {
            int minP = p + range;
            if (minP < rangeMin)
            {
                minP = rangeMin;
            }
            if (minP > rangeMax)
            {
                minP = rangeMin;
            }
            return minP;
        }

      /*  private SpaceObject[] GrabAllObjectsInRange(int minX, int maxX, int minZ, int maxZ)
        {
            int remainderX = minX % cellSize;
            minX /= cellSize;
            if(remainderX>0)
            {
                minX--;
            }
        }*/

     /*   private SpaceObject requestCell(int posX, int posZ)
        {
            int x = posX + cellsToAddForNormalization;
            int z = posZ + cellsToAddForNormalization;

            return cells[x, z];
        }*/
        float GetDistanceSquared(float x1, float z1, float x2, float z2)
        {
            float xrange = x2 - x1;
            float zrange = z2 - z1;
            float dist = xrange * xrange + zrange * zrange;
            return dist;
        }

        public List<SpaceObject> GetAll(int searchX, int searchZ, int range)
        {
            int r = Math.Abs(range);
            int temp = AssignAndRangeCheck(searchX, -r);
            int minCellX = NormalizeCell(temp);
            temp = AssignAndRangeCheck(searchX, r);
            int maxCellX = NormalizeCell(temp);
            temp = AssignAndRangeCheck(searchZ, -r);
            int minCellZ = NormalizeCell(temp);
            temp = AssignAndRangeCheck(searchX, r);
            int maxCellZ = NormalizeCell(temp);

            List<SpaceObject> objects = new List<SpaceObject>();
            int distanceSquared = range * range;
            for(int z= minCellZ; z<= maxCellZ;z++)
            {
                for(int x=minCellX; x<= maxCellX; x++)
                {
                    SpaceObject obj = cells[x, z];
                    while(obj != null)
                    {
                        if(GetDistanceSquared(obj.position.x, obj.position.z, searchX, searchZ) < distanceSquared)
                        {
                            objects.Add(obj);
                        }
                        obj = obj.next;
                    }
                }
            }
            
            return objects;
        }

        int GetCount(int cellX, int cellZ)
        {
            int count = 0;
            SpaceObject obj = cells[cellX, cellZ];

            while(obj != null)
            {
                count++;
                obj = obj.next;
            }
            return count;
        }
        public void PrintCountPerCell()
        {
            Console.WriteLine("---------------------------------------------------------");
            int numAcross = (rangeMax - rangeMin) / cellSize;
            for (int y = 0; y < numAcross; y++)
            {
                for (int x = 0; x < numAcross; x++)
                {
                    Console.Write("{0}\t", GetCount(x, y));
                }
                Console.Write("\n");
            }
            Console.WriteLine("---------------------------------------------------------");
        }
    }
}

