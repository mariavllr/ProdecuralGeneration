namespace Tessera
{
    /// <summary>
    /// Records some useful stats about the run
    /// </summary>
    internal class TesseraStats
    {
        // Fundamental timings
        public double createHelperTime;
        public double initializeTime;
        public double createPropagatorTime;
        public double initialConstraintsTime;
        public double skyboxTime;
        public double banBigTilesTime;
        public double runTime;
        public double postProcessTime;

        public double totalTile => createHelperTime 
            + initializeTime 
            + createPropagatorTime 
            + initialConstraintsTime 
            + skyboxTime 
            + banBigTilesTime 
            + runTime 
            + postProcessTime;

        // Details of the run
        public int stepCount;
    }
}
