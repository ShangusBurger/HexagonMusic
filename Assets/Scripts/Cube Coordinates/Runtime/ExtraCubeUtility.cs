using System.Collections.Generic;
using UnityEngine;

namespace CubeCoordinates
{
    public static class ExtraCubeUtility
    {
        // Determines which of the 6 hex directions best points toward the target hex
        public static int GetBestDirectionToTile(Coordinate origin, Coordinate target)
        {
            Vector3 diff = target.cube - origin.cube;

            // For each of the 6 directions, calculate how aligned it is with the target direction
            float[] alignments = new float[6];
            for (int dir = 0; dir < 6; dir++)
            {
                Vector3 dirVector = Cubes.directions[dir];
                // Dot product to measure alignment
                alignments[dir] = Vector3.Dot(diff.normalized, dirVector.normalized);
            }

            // Find the direction with the highest alignment
            int bestDir = 0;
            float bestAlignment = alignments[0];
            for (int i = 1; i < 6; i++)
            {
                if (alignments[i] > bestAlignment)
                {
                    bestAlignment = alignments[i];
                    bestDir = i;
                }
            }

            return bestDir;
        }
    }
}