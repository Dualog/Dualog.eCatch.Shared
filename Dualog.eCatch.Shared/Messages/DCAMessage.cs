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
    public class DCAMessage : Message
    {
        public IReadOnlyCollection<Haul> Hauls { get; }
        public string FishingActivity { get; }
        public string FishingPermission { get; }
        public string ArrivalHarbour { get; }
        public string PumpingFromBoat { get; }
        public string FishingLicense { get; }
        public string CurrentLatitude { get; }
        public string CurrentLongitude { get; }

        public DCAMessage(
            string fishingPermission, 
            string fishingActivity, 
            string arrivalHarbour, 
            IReadOnlyCollection<Haul> hauls, 
            DateTime sent, 
            string skipperName, 
            Ship ship, 
            int messageVersion = 1,
            string correctionCode = "",
            string pumpingFromBoat = "",
            string fishingLicense = "",
            string currentLatitude = "",
            string currentLongitude = "") : base(MessageType.DCA, sent, skipperName, ship, errorCode: correctionCode, messageVersion: messageVersion)
        {
            this.FishingPermission = fishingPermission;
            this.FishingActivity = fishingActivity;
            ArrivalHarbour = arrivalHarbour;
            Hauls = hauls;
            PumpingFromBoat = pumpingFromBoat;
            FishingLicense = fishingLicense;
            CurrentLatitude = currentLatitude;
            CurrentLongitude = currentLongitude;
        }

		public IReadOnlyList<FishFAOAndWeight> GetFishAndWeights()
		{
			return Hauls.SelectMany(m => m.FishDistribution).ToList();
		}

        protected override void WriteBody(StringBuilder sb)
        {
            sb.Append($"//QI/{FishingPermission}");
            sb.Append($"//AC/{FishingActivity}");
            if (!ArrivalHarbour.IsNullOrEmpty())
            {
                sb.Append($"//PO/{ArrivalHarbour}");
            }
            if (!FishingLicense.IsNullOrEmpty())
            {
                sb.Append($"//FL/{FishingLicense}");
            }

            sb.Append($"//XT/{CurrentLatitude}");
            sb.Append($"//XG/{CurrentLongitude}");

            Hauls.ForEach(c => WriteHaul(sb, c));
        }

        public override Dictionary<string, string> GetSummaryDictionary(EcatchLangauge lang)
        {
            var result = CreateBaseSummaryDictionary(lang);
            //result.Add("FishingPermission".Translate(lang), FishingPermission); //TODO Import FishingPermissions to get translation working
            result.Add("FishingActivity".Translate(lang), FishingActivity.ToString().ToFishingActivityName(lang));
            var i = 1;
            foreach (var haul in Hauls)
            {
                result.Add($"{"Haul".Translate(lang)} {i}", $"{haul.StartTime:dd.MM.yyyy HH:mm} - {haul.StopTime:dd.MM.yyyy HH:mm} ({haul.GetDuration()} {"Minutes".Translate(lang).ToLowerInvariant()})");
                result.Add($"{"Catch".Translate(lang)} for {"Haul".Translate(lang).ToLowerInvariant()} {i}", haul.FishDistribution.ToDetailedWeightAndFishNameSummary(lang));
                i++;
            }

            if (!string.IsNullOrEmpty(ArrivalHarbour))
            {
                result.Add("Arriving".Translate(lang), ArrivalHarbour.ToHarbourName());
            }
            if (!FishingLicense.IsNullOrEmpty())
            {
                result.Add("FishingLicense".Translate(lang), FishingLicense);
            }
            if(!string.IsNullOrEmpty(CurrentLatitude) && !string.IsNullOrEmpty(CurrentLongitude)){
                result.Add("CurrentPosition".Translate(lang), $"Lat: {CurrentLatitude}, Lon: {CurrentLongitude}");
            }
            return result;
        }

        private void WriteHaul(StringBuilder sb, Haul haul)
        {
            var startLat = haul.StartLatitude.ToWgs84Format(CoordinateType.Latitude);
            var startLon = haul.StartLongitude.ToWgs84Format(CoordinateType.Longitude);
            var stopLat = haul.StopLatitude.ToWgs84Format(CoordinateType.Latitude);
            var stopLon = haul.StopLongitude.ToWgs84Format(CoordinateType.Longitude);

            sb.Append("//TS");
            sb.Append($"//BD/{haul.StartTime.ToFormattedDate()}");
            sb.Append($"//BT/{haul.StartTime.ToFormattedTime()}");
            sb.Append($"//ZO/{haul.Zone}");
            sb.Append($"//LT/{startLat}");
            sb.Append($"//LG/{startLon}");
            sb.Append($"//GE/{haul.Tool}");
            
            if (haul.MaskWidth > 0)
            {
                sb.Append($"//ME/{haul.MaskWidth}");
            }

            if (haul.NumberOfTrawls > 0)
            {
                sb.Append($"//GS/{haul.NumberOfTrawls}");
            }
            
            if (haul.ExtraToolInfo > 0)
            {
                sb.Append($"//FO/{haul.ExtraToolInfo}");
            }
            //GO?
            sb.Append($"//GP/{haul.Problem}");
            sb.Append($"//XT/{stopLat}");
            sb.Append($"//XG/{stopLon}");
            sb.Append($"//DU/{haul.GetDuration()}");
            
            sb.Append($"//CA/{haul.FishDistribution.ToNAF()}");
            if (haul.AnimalCount.Any())
            {
                if (haul.FishDistribution.Any())
                {
                    sb.Append(" " + haul.AnimalCount.ToNAF());
                }
                else
                {
                    sb.Append(haul.AnimalCount.ToNAF());
                }
            }
            
            if (!haul.HerringType.IsNullOrEmpty())
            {
                sb.Append($"//SS/{haul.HerringType}");
            }
            
            if (FishingActivity.Equals("REL") && !PumpingFromBoat.IsNullOrEmpty())
            {
                sb.Append($"//TF/{PumpingFromBoat}");
            }
            
            if (!FishingLicense.IsNullOrEmpty())
            {
                sb.Append($"//FL/{FishingLicense}");
            }
        }

        public static DCAMessage ParseNAFFormat(int id, DateTime sent, IReadOnlyDictionary<string, string> values, List<IReadOnlyDictionary<string, string>> haulValues)
        {
            var pumpingFrom = string.Empty;
            if (haulValues.Any(x => x.ContainsKey("TF")))
            {
                var firstHaulWithPumping = haulValues.FirstOrDefault(x => x.ContainsKey("TF"));
                firstHaulWithPumping?.TryGetValue("TF", out pumpingFrom);
            }

            return new DCAMessage(
                values["QI"],
                values["AC"],
                values.ContainsKey("PO") ? values["PO"] : string.Empty,
                haulValues.Select(haul => 
                    new Haul(
                        (haul["BD"] + haul["BT"]).FromFormattedDateTime(),
                        (haul["BD"] + haul["BT"]).FromFormattedDateTime().AddMinutes(Convert.ToDouble(haul["DU"], CultureInfo.InvariantCulture)),
                        Convert.ToDouble(haul["LT"], CultureInfo.InvariantCulture), 
                        Convert.ToDouble(haul["LG"], CultureInfo.InvariantCulture),
                        Convert.ToDouble(haul["XT"], CultureInfo.InvariantCulture),
                        Convert.ToDouble(haul["XG"], CultureInfo.InvariantCulture),
                        haul["GE"], 
                        haul["GP"], 
                        MessageParsing.ParseFishWeights(haul["CA"]),
                        haul.ContainsKey("ME") ? Convert.ToInt32(haul["ME"]) : 0,
                        haul.ContainsKey("GS") ? Convert.ToInt32(haul["GS"]) : 0,
                        haul.ContainsKey("FO") ? Convert.ToInt32(haul["FO"]) : 0,
                        haul["ZO"],
                        haul.ContainsKey("SS") ? haul["SS"] : string.Empty,
                        MessageParsing.ParseAnimalCount(haul["CA"])
                        )
                    ).ToList(),
                sent,
                values["MA"], 
                new Ship(
                    values.ContainsKey("NA") ? values["NA"] : string.Empty,
                    values["RC"],
                    values.ContainsKey("XR") ? values["XR"] : string.Empty),
                Convert.ToInt32(values.ContainsKey("MV") ? values["MV"] : "0"),
                values.ContainsKey("RE") ? values["RE"] : string.Empty,
                pumpingFrom,
                fishingLicense: values.ContainsKey("FL") ? values["FL"] : string.Empty,
                currentLatitude: values.ContainsKey("XT") ? values["XT"] : string.Empty,
                currentLongitude: values.ContainsKey("XG") ? values["XG"] : string.Empty)
            {
                Id = id,
                ForwardTo = values.ContainsKey("FT") ? values["FT"] : string.Empty,
                SequenceNumber = values.ContainsKey("SQ") ? Convert.ToInt32(values["SQ"]) : 0
            };
        }
    }
}
