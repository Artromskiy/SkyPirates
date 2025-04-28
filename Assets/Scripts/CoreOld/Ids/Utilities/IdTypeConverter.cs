using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;

namespace DVG.Core.Ids.Utilities
{
    public abstract class IdTypeConverter<TId> : TypeConverter
        where TId : struct, IStringId
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string typedValue)
                return CreateFromSource(typedValue);
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(TId))
                return CreateFromSource((string)value);
            return base.ConvertTo(context, culture, value, destinationType);
        }

        protected abstract TId CreateFromSource(string srcData);
    }

    public abstract class IdJsonConverter<TId> : JsonConverter
        where TId : struct, IStringId
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var id = (TId)value;
            writer.WriteValue(id.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = (string)reader.Value;
            return CreateFromRawData(value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TId);
        }

        protected abstract TId CreateFromRawData(string type);
    }
}