namespace FWTCG.Data
{
    public enum RuneType
    {
        Blazing,
        Radiant,
        Verdant,
        Crushing,
        Chaos,
        Order
    }

    public static class RuneTypeExtensions
    {
        /// <summary>Full Chinese name wrapped in rich-text color tag for UI display.</summary>
        public static string ToColoredText(this RuneType rt)
        {
            string hex = UnityEngine.ColorUtility.ToHtmlStringRGB(
                FWTCG.UI.GameColors.GetRuneColor(rt));
            return $"<color=#{hex}>{rt.ToChinese()}</color>";
        }

        /// <summary>Full Chinese name used in all user-visible messages.</summary>
        public static string ToChinese(this RuneType rt)
        {
            switch (rt)
            {
                case RuneType.Blazing:  return "炽烈";
                case RuneType.Radiant:  return "灵光";
                case RuneType.Verdant:  return "翠意";
                case RuneType.Crushing: return "摧破";
                case RuneType.Chaos:    return "混沌";
                case RuneType.Order:    return "秩序";
                default: return rt.ToString();
            }
        }

        /// <summary>Single-character abbreviation used in rune zone labels.</summary>
        public static string ToShort(this RuneType rt)
        {
            switch (rt)
            {
                case RuneType.Blazing:  return "炽";
                case RuneType.Radiant:  return "灵";
                case RuneType.Verdant:  return "翠";
                case RuneType.Crushing: return "摧";
                case RuneType.Chaos:    return "混";
                case RuneType.Order:    return "序";
                default: return rt.ToString();
            }
        }
    }
}
