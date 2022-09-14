using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Dualog.eCatch.Shared.Enums;
using Dualog.eCatch.Shared.Extensions;
using Dualog.eCatch.Shared.Models;

namespace Dualog.eCatch.Shared.Messages
{
    public class COXMessage : Message
    {
        public string Zone { get; }
        public string DeliveryHarbour { get; }
        public string CatchArea { get; }
        public IReadOnlyList<FishFAOAndWeight> CatchSummarized { get; }
        public PositionAndTime PositionAndTime { get; }
        public IReadOnlyList<FishFAOAndWeight> CatchOnBoard { get; }
        public IReadOnlyList<FishFAOAndWeight> CatchDiscarded { get; }
        public int DaysFishing { get; }
        public string FishingLicense { get; }
        public string CurrentLatitude { get; }
        public string CurrentLongitude { get; }

        public COXMessage(
            DateTime sent,
            string zone,
            string skipperName,
            Ship ship,
            IReadOnlyList<FishFAOAndWeight> catchSummarized,
            IReadOnlyList<FishFAOAndWeight> catchOnBoard,
            PositionAndTime positionAndTime = null,
            int daysFishing = 0,
            string deliveryHarbour = "",
            string catchArea = "",
            string errorCode = "",
            string fishingLicense = "",
            string currentLatitude = "",
            string currentLongitude = "",
            IReadOnlyList<FishFAOAndWeight> catchDiscarded = null
            ) : base(MessageType.COX, sent, skipperName, ship, errorCode)
        {
            Zone = zone;
            DeliveryHarbour = deliveryHarbour;
            CatchArea = catchArea;
            CatchSummarized = catchSummarized;
            CatchOnBoard = catchOnBoard;
            CatchDiscarded = catchDiscarded;
            PositionAndTime = positionAndTime;
            DaysFishing = daysFishing;
            FishingLicense = fishingLicense;
            CurrentLatitude = currentLatitude;
            CurrentLongitude = currentLongitude;
        }

        protected override void WriteBody(StringBuilder sb)
        {
            if (!DeliveryHarbour.IsNullOrEmpty())
            {
                sb.Append($"//PO/{DeliveryHarbour}");
            }
            
            if (MessageFieldChecker.Cox.ShouldHaveOB(Zone))
            {
                sb.Append($"//OB/{CatchOnBoard.ToNAF()}");
            }

            if(Zone == Constants.Zones.NEAFC)
            {
                sb.Append($"//CA/{CatchSummarized.ToNAF()}");
                sb.Append($"//DF/{DaysFishing}");
            }

            if(!string.IsNullOrEmpty(CatchArea))
            {
                sb.Append($"//RA/{CatchArea}");
            }

            if(MessageFieldChecker.Cox.ShouldHaveCurrentPosition(Zone))
            {
                sb.Append($"//XT/{CurrentLatitude}");
                sb.Append($"//XG/{CurrentLongitude}");
            }

            if (Zone == Constants.Zones.Russia)
            {
                sb.Append($"//ZD/{PositionAndTime.DateTime.ToFormattedDate()}");
                sb.Append($"//ZT/{PositionAndTime.DateTime.ToFormattedTime()}");
                sb.Append($"//ZA/{PositionAndTime.Latitude.ToWgs84Format(CoordinateType.Latitude)}");
                sb.Append($"//ZG/{PositionAndTime.Longitude.ToWgs84Format(CoordinateType.Longitude)}");
            }
            if (CatchDiscarded != null && CatchDiscarded.Count > 0)
            {
                sb.Append($"//RJ/{CatchDiscarded.ToNAF()}");
            }
            if (!FishingLicense.IsNullOrEmpty())
            {
                sb.Append($"//FL/{FishingLicense}");
            }
        }

        public override Dictionary<string, string> GetSummaryDictionary(EcatchLangauge lang)
        {
            var result = CreateBaseSummaryDictionary(lang);

            if (Zone == Constants.Zones.NEAFC)
            {
                result.Add("CatchArea".Translate(lang), CatchArea);
                result.Add("FishingDays".Translate(lang), DaysFishing.ToString());
                result.Add("Catch".Translate(lang), CatchSummarized.ToDetailedWeightAndFishNameSummary(lang));
            }

            if (!string.IsNullOrEmpty(DeliveryHarbour))
            {
                result.Add("DeliveryHarbour".Translate(lang), DeliveryHarbour.ToHarbourName());
            }

            if (Zone == Constants.Zones.Russia || Zone == Constants.Zones.Island || Zone == Constants.Zones.FaroeIslands)
            {
                result.Add("FishOnBoard".Translate(lang), CatchOnBoard.ToDetailedWeightAndFishNameSummary(lang));
            }

            if (Zone == Constants.Zones.Greenland && CatchDiscarded != null)
            {
                result.Add("CatchDiscarded".Translate(lang), CatchDiscarded.ToDetailedWeightAndFishNameSummary(lang));
            }

            if (!FishingLicense.IsNullOrEmpty())
            {
                result.Add("FishingLicense".Translate(lang), FishingLicense);
            }

            if (!string.IsNullOrEmpty(CurrentLatitude) && !string.IsNullOrEmpty(CurrentLongitude))
            {
                result.Add("Position".Translate(lang), $"Lat: {CurrentLatitude}, Lon: {CurrentLongitude}");
            }

            return result;
        }

        public static COXMessage ParseNAFFormat(int id, DateTime sent, IReadOnlyDictionary<string, string> values)
        {
            return new COXMessage(
                sent,
                values.ContainsKey("FT") ? values["FT"] : string.Empty,
                values.ContainsKey("MA") ? values["MA"] : string.Empty,
                new Ship(
                    values.ContainsKey("NA") ? values["NA"] : string.Empty,
                    values["RC"],
                    values.ContainsKey("XR") ? values["XR"] : string.Empty),
                values.ContainsKey("CA") ? MessageParsing.ParseFishWeights(values["CA"]) : new List<FishFAOAndWeight>(),
                values.ContainsKey("OB") ? MessageParsing.ParseFishWeights(values["OB"]) : new List<FishFAOAndWeight>(),
                values.ContainsKey("ZD") && values.ContainsKey("ZT") ? new PositionAndTime((values["ZD"] + values["ZT"]).FromFormattedDateTime(), Convert.ToDouble(values["ZA"], CultureInfo.InvariantCulture), Convert.ToDouble(values["ZG"], CultureInfo.InvariantCulture)) : null,
                values.ContainsKey("DF") ? Convert.ToInt32(values["DF"]) : 0,
                values.ContainsKey("PO") ? values["PO"] : string.Empty,
                values.ContainsKey("RA") ? values["RA"] : string.Empty,
                values.ContainsKey("RE") ? values["RE"] : string.Empty,
                fishingLicense: values.ContainsKey("FL") ? values["FL"] : string.Empty,
                currentLatitude: values.ContainsKey("XT") ? values["XT"] : string.Empty,
                currentLongitude: values.ContainsKey("XG") ? values["XG"] : string.Empty,
                catchDiscarded: values.ContainsKey("RJ") ? MessageParsing.ParseFishWeights(values["RJ"]) : new List<FishFAOAndWeight>())
            {
                Id = id,
                ForwardTo = values.ContainsKey("FT") ? values["FT"] : string.Empty,
                SequenceNumber = values.ContainsKey("SQ") ? Convert.ToInt32(values["SQ"]) : 0
            };
        }
    }
}
