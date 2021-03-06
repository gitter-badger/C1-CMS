using System;
using System.ComponentModel;
using System.Globalization;

namespace Composite.Core.ResourceSystem
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
    [TypeConverter(typeof(IconSizeConverter))]
    public enum IconSize
    {
        /// <exclude />
        Normal = 16,

        /// <exclude />
        Large = 24,

        /// <exclude />
        XLarge = 32
    }


    internal class IconSizeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string val = value as string;

            val = val.ToLowerInvariant();

            switch (val)
            {
                case "normal":
                    return IconSize.Normal;

                case "large":
                    return IconSize.Large;

                case "xlarge":
                    return IconSize.XLarge;

                default:
                    throw new FormatException(String.Format("{0} is not a valid IconSize - use Normal, Large or XLarge", value));
            }
        }
    }
}
