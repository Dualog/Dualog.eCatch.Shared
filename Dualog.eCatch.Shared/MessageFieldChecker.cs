using System.Linq;

namespace Dualog.eCatch.Shared
{
    /// <summary>
    /// Class containing checks for all message types. Each message has some fields that are required for a zone(who the message should be forwarded to).
    /// </summary>
    public static class MessageFieldChecker
    {
        public static class ZoneUsesFormat
        {
            /// <summary>
            /// Zones requiring the LT and LG fields. LT using the format +/- DD.ddd (WGS-84), and LG using the format +/- DDD.ddd (WGS-84)
            /// </summary>
            public static bool LtLg(string forwardTo) => ZonesUsingLtLg.Contains(forwardTo);
            /// <summary>
            /// Zones requiring the LA and LO fields. LA using the format N/SGGDD (WGS-84), and LO using the format E/WGGGDD (WGS-84)
            /// </summary>
            public static bool LaLo(string forwardTo) => ZonesUsingLaLo.Contains(forwardTo) || string.IsNullOrEmpty(forwardTo);

            private static readonly string[] ZonesUsingLtLg =
            {
                Constants.Zones.EU,
                Constants.Zones.FaroeIslands,
                Constants.Zones.GBR,
                Constants.Zones.Island,
                Constants.Zones.Russia,
            };
            private static readonly string[] ZonesUsingLaLo =
            {
                Constants.Zones.Norway,
                Constants.Zones.Svalbard,
                Constants.Zones.NEAFC,
                Constants.Zones.Greenland,
                Constants.Zones.SvalbardTerritorialWaters
            };
        }

        public static class Coe
        {
            /// <summary>
            /// Checks if current position is required
            /// </summary>
            /// <param name="forwardTo"></param>
            /// <returns></returns>
            public static bool XtXg(string forwardTo) => 
                forwardTo != Constants.Zones.Russia 
                && forwardTo != Constants.Zones.EU 
                && forwardTo != Constants.Zones.GBR 
                && forwardTo != Constants.Zones.NEAFC;
            /// <summary>
            /// Checks if target species is required
            /// </summary>
            /// <param name="forwardTo"></param>
            /// <returns></returns>
            public static bool Ds(string forwardTo) => forwardTo != Constants.Zones.Russia;
            /// <summary>
            /// Checks if catch area is required
            /// </summary>
            /// <param name="forwardTo"></param>
            /// <returns></returns>
            public static bool Ra(string forwardTo) => forwardTo != Constants.Zones.Russia;
        }

        public static class Cox
        {
            public static bool ShouldHaveOB(string currentZone) => 
                currentZone == Constants.Zones.Island
                || currentZone == Constants.Zones.FaroeIslands
                || currentZone == Constants.Zones.Norway
                || currentZone == Constants.Zones.Skagerrak
                || currentZone == Constants.Zones.Svalbard
                || currentZone == Constants.Zones.SvalbardTerritorialWaters;

             public static bool ShouldHaveCurrentPosition(string currentZone) => 
                currentZone == Constants.Zones.Norway
                || currentZone == Constants.Zones.Skagerrak
                || currentZone == Constants.Zones.Svalbard
                || currentZone == Constants.Zones.SvalbardTerritorialWaters
                || currentZone == Constants.Zones.GBR
                || currentZone == Constants.Zones.EU
                || currentZone == Constants.Zones.Island
                || currentZone == Constants.Zones.FaroeIslands;
        }
    }
}
