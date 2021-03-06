﻿namespace Dualog.eCatch.Shared.Enums
{
    /// <summary>
    /// Message types for electronich reporting
    /// </summary>    
    public enum MessageType
    {
        /// <summary>
        /// Departure report
        /// </summary>
        DEP,
        /// <summary>
        /// Port report 
        /// </summary>
        POR,
        /// <summary>
        /// Detailed Catch and Activity
        /// </summary>
        DCA,
        /// <summary>
        /// Daily catch
        /// </summary>
        CAT,
        /// <summary>
        /// Catch on Entry 
        /// </summary>
        COE,
        /// <summary>
        /// Catch on Exit
        /// </summary>
        COX,
        /// <summary>
        /// Control Point/Area
        /// </summary>
        CON,
        /// <summary>
        /// Transhipment
        /// </summary>
        TRA,
        /// <summary>
        /// Manual position message
        /// </summary>
        MAN,
        /// <summary>
        /// Test message
        /// </summary>
        AUD,
        /// <summary>
        /// Landing message
        /// </summary>
        LAN,
        /// <summary>
        /// Return message
        /// </summary>
        RET,
        /// <summary>
        /// HiSampling message entering ship in pole 1
        /// Message is sent with DEP
        /// </summary>
        HIA,
        /// <summary>
        /// HiSampling message entering ship in pole 2
        /// Message is sent with DCA
        /// </summary>
        HIF,
        /// <summary>
        /// HiSampling message informing HI that samples has been landed
        /// </summary>
        HIL
    }
}
