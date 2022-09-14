using System;
using System.Collections.Generic;
using System.Text;
using Dualog.eCatch.Shared.Enums;
using Dualog.eCatch.Shared.Extensions;
using Dualog.eCatch.Shared.Models;

namespace Dualog.eCatch.Shared.Messages
{
    public class COEMessage : Message
    {
        public string CatchArea { get; }
        public DateTime FishStart { get; }
        public string TargetSpecies { get; }
        public string CurrentLatitude { get; }
        public string CurrentLongitude { get; }
        public string FishStartLatitude { get; }
        public string FishStartLongitude { get; }
        public IReadOnlyList<FishFAOAndWeight> FishOnBoard { get; }

        public DateTime CrossBorderTime { get; private set; }
        public string CrossBorderLatitude { get; private set; }
        public string CrossBorderLongitude { get; private set; }
        public string FishingLicense { get; private set; }

        public COEMessage(
                        DateTime sent,
                        string catchArea,
                        DateTime fishStart,
                        string targetSpecies,
                        string currentLatitude,
                        string currentLongitude,
                        string fishStartLatitude,
                        string fishStartLongitude,
                        IReadOnlyList<FishFAOAndWeight> fishOnBoard,
                        string skipperName,
                        Ship ship,
                        string cancelCode = "",
                        string fishingLicense = "") : base(MessageType.COE, sent, skipperName, ship, errorCode: cancelCode)
        {
            CatchArea = catchArea;
            FishStart = fishStart;
            TargetSpecies = targetSpecies;
            CurrentLatitude = currentLatitude;
            CurrentLongitude = currentLongitude;
            FishOnBoard = fishOnBoard;
            FishStartLatitude = fishStartLatitude;
            FishStartLongitude = fishStartLongitude;
            FishingLicense = fishingLicense;
        }

        public void SetCrossBorderInfo(DateTime crossBorderTime, string crossBorderLat, string crossBorderLon)
        {
            CrossBorderTime = crossBorderTime;
            CrossBorderLatitude = crossBorderLat;
            CrossBorderLongitude = crossBorderLon;
        }
        protected override void WriteBody(StringBuilder sb)
        {
            sb.Append($"//OB/{FishOnBoard.ToNAF()}");
            
            if (ForwardTo == Constants.Zones.NEAFC)
            {
                sb.Append($"//LA/{CurrentLatitude}");
                sb.Append($"//LO/{CurrentLongitude}");
            }
            else
            {
                sb.Append($"//XT/{CurrentLatitude}");
                sb.Append($"//XG/{CurrentLongitude}");
            }
            
            if (MessageFieldChecker.Coe.Ra(ForwardTo))
            {
                sb.Append($"//RA/{CatchArea}");
            }
            sb.Append($"//PD/{FishStart.ToFormattedDate()}");
            sb.Append($"//PT/{FishStart.ToFormattedTime()}");
            
            if(ForwardTo != Constants.Zones.NEAFC){
                if (MessageFieldChecker.ZoneUsesFormat.LtLg(ForwardTo))
                {
                    sb.Append($"//LT/{FishStartLatitude}");
                    sb.Append($"//LG/{FishStartLongitude}");
                }
                else if (MessageFieldChecker.ZoneUsesFormat.LaLo(ForwardTo))
                {
                    sb.Append($"//LA/{FishStartLatitude}");
                    sb.Append($"//LO/{FishStartLongitude}");
                }
            }
            if (ForwardTo == Constants.Zones.Russia)
            {
                sb.Append($"//ZD/{CrossBorderTime.ToFormattedDate()}");
                sb.Append($"//ZT/{CrossBorderTime.ToFormattedTime()}");
                sb.Append($"//ZA/{CrossBorderLatitude}");
                sb.Append($"//ZG/{CrossBorderLongitude}");
            }
            if (MessageFieldChecker.Coe.Ds(ForwardTo))
            {
                sb.Append($"//DS/{TargetSpecies}");
            }
            if (!FishingLicense.IsNullOrEmpty())
            {
                sb.Append($"//FL/{FishingLicense}");
            }
        }

        public override Dictionary<string, string> GetSummaryDictionary(EcatchLangauge lang)
        {
            var result = CreateBaseSummaryDictionary(lang);

            if (!string.IsNullOrEmpty(FishStartLatitude) && !string.IsNullOrEmpty(FishStartLongitude))
            {
                result.Add("FishStart".Translate(lang), $"Lat: {FishStartLatitude}, Lon: {FishStartLongitude}");
            }

            result.Add("ArrivingAtPosition".Translate(lang), $"{FishStart:dd.MM.yyyy HH:mm} UTC");

            if (!string.IsNullOrEmpty(CurrentLatitude) && !string.IsNullOrEmpty(CurrentLongitude))
            {
                result.Add("Position".Translate(lang), $"Lat: {CurrentLatitude}, Lon: {CurrentLongitude}");
            }

            if (!string.IsNullOrEmpty(CatchArea))
            {
                result.Add("CatchArea".Translate(lang), CatchArea);
            }

            if (!string.IsNullOrEmpty(TargetSpecies))
            {
                result.Add("TargetSpecies".Translate(lang), TargetSpecies.ToFishName(lang));
            }

            if (!string.IsNullOrEmpty(FishingLicense))
            {
                result.Add("FishingLicense".Translate(lang), FishingLicense);
            }

            result.Add("FishOnBoard".Translate(lang), FishOnBoard.ToDetailedWeightAndFishNameSummary(lang));

            return result;
        }

        public static COEMessage ParseNAFFormat(int id, DateTime sent, IReadOnlyDictionary<string, string> values)
        {
            var forwardTo = values.ContainsKey("FT") ? values["FT"] : string.Empty;
            var fishStartLat = "";
            var fishStartLon = "";
            var currentLat = "";
            var currentLon = "";
            if (forwardTo != Constants.Zones.NEAFC && values.ContainsKey("XT") && values.ContainsKey("XG"))
            {
                currentLat = values["XT"];
                currentLon = values["XG"];
            }

            if (MessageFieldChecker.ZoneUsesFormat.LtLg(forwardTo) && values.ContainsKey("LT") && values.ContainsKey("LG"))
            {
                fishStartLat = values["LT"];
                fishStartLon = values["LG"];
            }
            else if (MessageFieldChecker.ZoneUsesFormat.LaLo(forwardTo) && values.ContainsKey("LA") && values.ContainsKey("LO"))
            {
                //NEAFC Uses the LA/LO as current position and not fish start...
                if (forwardTo == Constants.Zones.NEAFC)
                {
                    currentLon = values["LA"];
                    currentLat = values["LO"];
                }
                else
                {
                    fishStartLat = values["LA"];
                    fishStartLon = values["LO"];
                }
            }
            var coeMessage = new COEMessage(
                sent,
                values.ContainsKey("RA") ? values["RA"] : string.Empty,
                (values.ContainsKey("PD") && values.ContainsKey("PT")) ? (values["PD"] + values["PT"]).FromFormattedDateTime() : DateTime.MinValue,
                values.ContainsKey("DS") ? values["DS"] : string.Empty,
                currentLat,
                currentLon,
                fishStartLat,
                fishStartLon,
                MessageParsing.ParseFishWeights(values["OB"]),
                values.ContainsKey("MA") ? values["MA"] : string.Empty,
                new Ship(
                    values.ContainsKey("NA") ? values["NA"] : string.Empty,
                    values["RC"],
                    values.ContainsKey("XR") ? values["XR"] : string.Empty),
                values.ContainsKey("RE") ? values["RE"] : string.Empty,
                fishingLicense: values.ContainsKey("FL") ? values["FL"] : string.Empty)
            {
                Id = id,
                ForwardTo = forwardTo,
                SequenceNumber = values.ContainsKey("SQ") ? Convert.ToInt32(values["SQ"]) : 0
            };
            if (forwardTo == Constants.Zones.Russia)
            {
                var time = (values["ZD"] + values["ZT"]).FromFormattedDateTime();
                var borderLat = values["ZA"];
                var borderLon = values["ZG"];
                coeMessage.SetCrossBorderInfo(time, borderLat, borderLon);
            }
            return coeMessage;
        }
    }
}
