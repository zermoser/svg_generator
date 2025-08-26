using System.Collections.Generic;

namespace TimelineChartAPI.Services
{
    public interface ILocalizationService
    {
        Dictionary<string, string> GetTexts(string language);
    }

    public class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["th"] = new Dictionary<string, string>
            {
                ["xAxisLabel"] = "สิ้นปีกรมธรรม์ที่",
                ["premiumEnd"] = "ชำระเบี้ยครบ",
                ["atAge"] = "ครบอายุ",
                ["coverage"] = "ความคุ้มครองชีวิต : จำนวนที่มากกว่าระหว่าง 100% ของทุนประกันภัย",
                ["or"] = "หรือ มูลค่าเวนคืนเงินสด หรือ เบี้ยประกันภัยสะสม"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["xAxisLabel"] = "End of Year",
                ["premiumEnd"] = "Premium Payment Finished",
                ["atAge"] = "At age",
                ["coverage"] = "Death coverage*",
                ["or"] = "or Cash Value or Accumulated Premium"
            }
        };

        public Dictionary<string, string> GetTexts(string language)
        {
            return _translations.TryGetValue(language, out var texts) 
                ? texts 
                : _translations["en"];
        }
    }
}
