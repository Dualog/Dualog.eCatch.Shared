using System;
using System.Collections.Generic;
using System.Text;
using Dualog.eCatch.Shared.Enums;
using Dualog.eCatch.Shared.Extensions;
using Dualog.eCatch.Shared.Models;

namespace Dualog.eCatch.Shared.Messages
{
    public class TRAMessage : Message
    {
        public DateTime ReloadDateTime { get; }
        public ReloadingPurpose ReloadingPurpose { get; }
        public string Latitude { get; }
        public string Longitude { get; }
        public IReadOnlyList<FishFAOAndWeight> FishOnBoard { get; }
        public IReadOnlyList<FishFAOAndWeight> TransferedFish { get; }
        public string RadioCallSignalForOtherParty { get; }
        public string HarbourCode { get; }
        public string FishingLicense { get; }
        public string CurrentLatitude { get; }
        public string CurrentLongitude { get; }

        public TRAMessage(
            DateTime sent, 
            ReloadingPurpose reloadingPurpose, 
            string latitude, 
            string longitude,
            DateTime reloadDateTime, 
            IReadOnlyList<FishFAOAndWeight> fishOnBoard, 
            IReadOnlyList<FishFAOAndWeight> transferedFish, 
            string radioCallSignalForOtherParty,
            string skipperName,
            Ship ship,
            string currentLat,
            string currentLon,
            string cancelCode = "",
            string harbourCode = "",
            string fishingLicense = "") : base(MessageType.TRA, sent, skipperName, ship, errorCode:cancelCode)
        {
            this.ReloadingPurpose = reloadingPurpose;
            Latitude = latitude;
            Longitude = longitude;
            ReloadDateTime = reloadDateTime;
            FishOnBoard = fishOnBoard;
            TransferedFish = transferedFish;
            RadioCallSignalForOtherParty = radioCallSignalForOtherParty;
            HarbourCode = harbourCode;
            FishingLicense = fishingLicense;
            CurrentLatitude = currentLat;
            CurrentLongitude = currentLon;
        }

        protected override void WriteBody(StringBuilder sb)
        {
            if (MessageFieldChecker.ZoneUsesFormat.LtLg(ForwardTo))
            {
                sb.Append($"//LT/{Latitude}");
                sb.Append($"//LG/{Longitude}");
            }
            else if (MessageFieldChecker.ZoneUsesFormat.LaLo(ForwardTo))
            {
                sb.Append($"//LA/{Latitude}");
                sb.Append($"//LO/{Longitude}");
                sb.Append($"//XT/{CurrentLatitude}");
                sb.Append($"//XG/{CurrentLongitude}");
            }
            sb.Append($"//OB/{FishOnBoard.ToNAF()}");
            sb.Append($"//KG/{TransferedFish.ToNAF()}");
            sb.Append(ReloadingPurpose == ReloadingPurpose.Receiving
                ? $"//TF/{RadioCallSignalForOtherParty.ToUpper().Trim()}"
                : $"//TT/{RadioCallSignalForOtherParty.ToUpper().Trim()}");
            if (!HarbourCode.IsNullOrEmpty())
            {
                sb.Append($"//PO/{HarbourCode}");
            }
            sb.Append($"//PD/{ReloadDateTime.ToFormattedDate()}");
            sb.Append($"//PT/{ReloadDateTime.ToFormattedTime()}");
            if (!FishingLicense.IsNullOrEmpty())
            {
                sb.Append($"//FL/{FishingLicense}");
            }
        }

        public override Dictionary<string, string> GetSummaryDictionary(EcatchLangauge lang)
        {
            var result = CreateBaseSummaryDictionary(lang);

            result.Add("Position".Translate(lang), $"Lat: {Latitude}, Lon: {Longitude}");
            result.Add(
                ReloadingPurpose == ReloadingPurpose.Receiving
                    ? "TransferedFrom".Translate(lang)
                    : "TransferedTo".Translate(lang), $"{RadioCallSignalForOtherParty} {ReloadDateTime:dd.MM.yyyy HH:mm}");
            result.Add("TransferedFish".Translate(lang), TransferedFish.ToDetailedWeightAndFishNameSummary(lang));
            result.Add("FishOnBoard".Translate(lang), FishOnBoard.ToDetailedWeightAndFishNameSummary(lang));
            if(!CurrentLatitude.IsNullOrEmpty() && !CurrentLongitude.IsNullOrEmpty())
            {
                result.Add("Position".Translate(lang), $"Lat: {CurrentLatitude}, Lon: {CurrentLongitude}");
            }
            if (!HarbourCode.IsNullOrEmpty())
            {
                result.Add("Harbour".Translate(lang), HarbourCode);
            }
            if (!string.IsNullOrEmpty(FishingLicense))
            {
                result.Add("FishingLicense".Translate(lang), FishingLicense);
            }
            return result;
        }

        public static TRAMessage ParseNAFFormat(int id, DateTime sent, IReadOnlyDictionary<string, string> values)
        {
            var lat = "";
            var lon = "";
            var currentLat = "";
            var currentLon = "";
            if (values.ContainsKey("LA"))
            {
                lat = values["LA"];
            }
            if (values.ContainsKey("LO"))
            {
                lon = values["LO"];
            }
            if (values.ContainsKey("LT"))
            {
                lat = values["LT"];
            }
            if (values.ContainsKey("LG"))
            {
                lon = values["LG"];
            }
            if(values.ContainsKey("XT") && values.ContainsKey("XG"))
            {
                currentLat = values["XT"];
                currentLon = values["XG"];
            }
            //TT is set if the boat is delivering to another boat
            ReloadingPurpose purpose = values.ContainsKey("TT")
                ? ReloadingPurpose.Delivering
                : ReloadingPurpose.Receiving;
            return new TRAMessage(
                sent, 
                purpose,
                lat,
                lon,
                (values.ContainsKey("PD") && values.ContainsKey("PT")) ? (values["PD"] + values["PT"]).FromFormattedDateTime() : DateTime.MinValue,
                MessageParsing.ParseFishWeights(values.ContainsKey("OB") ? values["OB"] : string.Empty),
                MessageParsing.ParseFishWeights(values["KG"]),
                purpose == ReloadingPurpose.Receiving ? values["TF"] : values["TT"],
                values["MA"], 
                new Ship(
                    values.ContainsKey("NA") ? values["NA"] : string.Empty,
                    values["RC"],
                    values.ContainsKey("XR") ? values["XR"] : string.Empty),
                !currentLat.IsNullOrEmpty() ? currentLat : string.Empty,
                !currentLon.IsNullOrEmpty() ? currentLon : string.Empty,
                values.ContainsKey("RE") ? values["RE"] : string.Empty,
                values.ContainsKey("PO") ? values["PO"] : string.Empty,
                fishingLicense: values.ContainsKey("FL") ? values["FL"] : string.Empty)
            {
                Id = id,
                ForwardTo = values.ContainsKey("FT") ? values["FT"] : string.Empty,
                SequenceNumber = values.ContainsKey("SQ") ? Convert.ToInt32(values["SQ"]) : 0
            };
        }
    }
}
