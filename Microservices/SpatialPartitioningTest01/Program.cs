using SpatialPartitioningTest01;
using SpatialPartitionPattern;
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
        public bool isNewlySpawned, isNewlyRemoved;

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
        public Asteroid Duplicate()
        {
            Asteroid ast = new Asteroid();
            ast.spaceObject.position = spaceObject.position;
            return ast;
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

            SpatialPartitionPattern.VisibilityGrid partition = new SpatialPartitionPattern.VisibilityGrid(range, 500);

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
                if (RunCaptureTest((int)center.x, (int)center.z, dist, ast, partition) == false)
                {
                    Console.WriteLine("**** failed **** iteration {0}, x: {1}, z: {2}, dist: {3}", testCount, (int)center.x, (int)center.z, dist);
                }
                else
                {
                    successCount++;
                }

                Console.WriteLine("Num successes: {0} out of {1} test runs", successCount, numTestRuns);
            }

            if (RunDeleteTest1((int)0, (int)0, 500, ast, partition) == false)
            {
                Console.WriteLine("RunDeleteTest1 failed");
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
        static int GetNumAsteroidsClose(Asteroid[] ast, int searchX, int searchZ, int range)
        {
            int count = 0;
            Vector3 center = new Vector3(searchX, 0, searchZ);
            int dist = range * range;

            foreach (var t in ast)
            {
                if (Vector3.DistanceSquared(center, t.spaceObject.position) < dist)
                {
                    count++;
                }
            }
            return count;
        }
        static bool RunCaptureTest(int centerX, int centerZ, int range, Asteroid[] ast, SpatialPartitionPattern.VisibilityGrid partition)
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
        static bool RunDeleteTest1(int centerX, int centerZ, int range, Asteroid[] ast, SpatialPartitionPattern.VisibilityGrid partition)
        {
            partition.ClearAll();
            partition.Add(ast[0].spaceObject);
            Asteroid astDup = ast[0].Duplicate();
            partition.Add(astDup.spaceObject);

            Vector3 center = ast[0].spaceObject.position;
            int dist = 500;
            List<SpaceObject> myList = partition.GetAll((int)center.x, (int)center.z, dist);

            int countToFind = 2;
            foreach (var obj in myList)
            {
                if (obj == ast[0].spaceObject)
                {
                    countToFind--;
                }
                else if (obj == astDup.spaceObject)
                {
                    countToFind--;
                }
            }

            if (countToFind != 0)
            {
                Console.WriteLine("RunDeleteTest1:: invalid storage of vectors ****");
            }
            partition.Remove(astDup.spaceObject);
            List<SpaceObject> myList2 = partition.GetAll((int)center.x, (int)center.z, dist);

            if (myList2.Count != myList.Count - 1)
            {
                Console.WriteLine("RunDeleteTest1:: wrong number of items deleted");
            }
            else
            {
                Console.WriteLine("RunDeleteTest1:: SUCCESS");
                return true;
            }

            return false;
        }
        class Observer : IMovementObserver
        {
            Vector3 location;
            public void SetPosition(int x, int z) { location.x = x; location.z = z; }
            public Vector3 GetPosition() { return location; }
        }
        static bool RunEventCallbacksTest(int centerX, int centerZ, int range, Asteroid[] ast, SpatialPartitionPattern.VisibilityGrid partition)
        {
            partition.ClearAll();
            partition.Tick();// must tick

            
            Observer ob = new Observer();
            ob.Range = 200;
            ob.SetPosition(centerX, centerZ);

            return true;
        }
    }
    

}


