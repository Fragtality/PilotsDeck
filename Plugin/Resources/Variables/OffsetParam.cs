using System;

namespace PilotsDeck.Resources.Variables
{
    public enum OffsetType
    {
        INTEGER,
        FLOAT,
        STRING,
        BIT
    }

    public class OffsetParam
    {
        public int Address { get; }
        public int Length { get; }
        public OffsetType Type { get; }
        public bool Signed { get; }
        public int BitNum { get; }

        public OffsetParam(int _address, int _length, OffsetType _type = OffsetType.INTEGER, bool _signed = false, int _bit = 0)
        {
            Address = _address;
            Length = _length;
            Type = _type;
            Signed = _signed;
            BitNum = _bit;
        }

        public OffsetParam(string paramString)
        {
            string[] parameters = paramString.Split(':');

            Address = Convert.ToInt32(parameters[0], 16);
            Length = Convert.ToInt32(parameters[1]);
            if (parameters.Length > 2)
            {
                switch (parameters[2].ToLower())
                {
                    case "s":
                        Type = OffsetType.STRING;
                        break;
                    case "f":
                        Type = OffsetType.FLOAT;
                        break;
                    case "b":
                        Type = OffsetType.BIT;
                        BitNum = 0;
                        break;
                    default:
                        Type = OffsetType.INTEGER;
                        Signed = false;
                        break;
                }

                if (parameters.Length > 3)
                {
                    if (Type == OffsetType.INTEGER && parameters[3].ToLower() == "s")
                        Signed = true;

                    if (Type == OffsetType.BIT && int.TryParse(parameters[3], out int bitnum))
                        BitNum = bitnum;
                }

            }
            else
            {
                Type = OffsetType.INTEGER;
                Signed = false;
            }
        }
    }
}
