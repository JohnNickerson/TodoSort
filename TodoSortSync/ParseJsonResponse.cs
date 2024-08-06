    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
namespace AssimilationSoftware.TodoSort.Sync
{

    public partial class PocketItem
    {
        [JsonProperty("status")]
        public long Status { get; set; }

        [JsonProperty("complete")]
        public long Complete { get; set; }

        [JsonProperty("list")]
        public Dictionary<string, List> List { get; set; }

        [JsonProperty("error")]
        public object Error { get; set; }

        [JsonProperty("search_meta")]
        public SearchMeta SearchMeta { get; set; }

        [JsonProperty("since")]
        public long Since { get; set; }
    }

    public partial class List
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("resolved_id")]
        public string ResolvedId { get; set; }

        [JsonProperty("given_url")]
        public string GivenUrl { get; set; }

        [JsonProperty("given_title")]
        public string GivenTitle { get; set; }

        [JsonProperty("favorite")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Favorite { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Status { get; set; }

        [JsonProperty("time_added")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TimeAdded { get; set; }

        [JsonProperty("time_updated")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TimeUpdated { get; set; }

        [JsonProperty("time_read")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TimeRead { get; set; }

        [JsonProperty("time_favorited")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TimeFavorited { get; set; }

        [JsonProperty("sort_id")]
        public long SortId { get; set; }

        [JsonProperty("resolved_title")]
        public string ResolvedTitle { get; set; }

        [JsonProperty("resolved_url")]
        public string ResolvedUrl { get; set; }

        [JsonProperty("excerpt")]
        public string Excerpt { get; set; }

        [JsonProperty("is_article")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long IsArticle { get; set; }

        [JsonProperty("is_index")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long IsIndex { get; set; }

        [JsonProperty("has_video")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long HasVideo { get; set; }

        [JsonProperty("has_image")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long HasImage { get; set; }

        [JsonProperty("word_count")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long WordCount { get; set; }

        [JsonProperty("lang")]
        public Lang Lang { get; set; }

        [JsonProperty("time_to_read", NullValueHandling = NullValueHandling.Ignore)]
        public long? TimeToRead { get; set; }

        [JsonProperty("top_image_url", NullValueHandling = NullValueHandling.Ignore)]
        public string TopImageUrl { get; set; }

        [JsonProperty("authors", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Author> Authors { get; set; }

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public PurpleImage Image { get; set; }

        [JsonProperty("images", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ImageValue> Images { get; set; }

        [JsonProperty("domain_metadata", NullValueHandling = NullValueHandling.Ignore)]
        public DomainMetadata DomainMetadata { get; set; }

        [JsonProperty("listen_duration_estimate")]
        public long ListenDurationEstimate { get; set; }

        [JsonProperty("videos", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Video> Videos { get; set; }

        [JsonProperty("amp_url", NullValueHandling = NullValueHandling.Ignore)]
        public string AmpUrl { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public Tags Tags { get; set; }
    }

    public partial class Author
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("author_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long AuthorId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public partial class DomainMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("logo")]
        public string Logo { get; set; }

        [JsonProperty("greyscale_logo")]
        public string GreyscaleLogo { get; set; }
    }

    public partial class PurpleImage
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }

        [JsonProperty("width")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Width { get; set; }

        [JsonProperty("height")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Height { get; set; }
    }

    public partial class ImageValue
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("image_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long ImageId { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }

        [JsonProperty("width")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Width { get; set; }

        [JsonProperty("height")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Height { get; set; }

        [JsonProperty("credit")]
        public Credit Credit { get; set; }

        [JsonProperty("caption")]
        public string Caption { get; set; }
    }

    public partial class Tags
    {
        [JsonProperty("cracked: all posts")]
        public CrackedAllPosts CrackedAllPosts { get; set; }

        [JsonProperty("ifttt")]
        public CrackedAllPosts Ifttt { get; set; }
    }

    public partial class CrackedAllPosts
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("tag")]
        public Tag Tag { get; set; }
    }

    public partial class Video
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("video_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long VideoId { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }

        [JsonProperty("width")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Width { get; set; }

        [JsonProperty("height")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Height { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Type { get; set; }

        [JsonProperty("vid")]
        public string Vid { get; set; }

        [JsonProperty("length")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Length { get; set; }
    }

    public partial class SearchMeta
    {
        [JsonProperty("search_type")]
        public string SearchType { get; set; }
    }

    public enum Credit { ApPhotoChrisRugaber, Empty, GettyPaigeVickersForVox, JetskeFlickr, NickStockton, PaulineDakinPhotoPenguin, PhotoDaveBenettGettyImages, PhotoIllustrationBrU00E1UlioAmado, PhotoIllustrationTheCutPhotosGettyImages, PhotoMarcPiaseckiWireImage, UsgsWashingtonStateUniversity };

    public enum Lang { Empty, En };

    public enum Tag { CrackedAllPosts, Ifttt };

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                CreditConverter.Singleton,
                LangConverter.Singleton,
                TagConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    internal class CreditConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Credit) || t == typeof(Credit?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "":
                    return Credit.Empty;
                case "AP Photo\\/Chris Rugaber":
                    return Credit.ApPhotoChrisRugaber;
                case "Getty\\/Paige Vickers for Vox":
                    return Credit.GettyPaigeVickersForVox;
                case "Jetske\\/Flickr":
                    return Credit.JetskeFlickr;
                case "Nick Stockton":
                    return Credit.NickStockton;
                case "Pauline Dakin (Photo: Penguin)":
                    return Credit.PaulineDakinPhotoPenguin;
                case "Photo illustration  Br\\u00e1ulio Amado":
                    return Credit.PhotoIllustrationBrU00E1UlioAmado;
                case "Photo-Illustration: The Cut; Photos Getty Images":
                    return Credit.PhotoIllustrationTheCutPhotosGettyImages;
                case "Photo: Dave Benett\\/Getty Images":
                    return Credit.PhotoDaveBenettGettyImages;
                case "Photo: Marc Piasecki\\/WireImage":
                    return Credit.PhotoMarcPiaseckiWireImage;
                case "USGS\\/Washington State University":
                    return Credit.UsgsWashingtonStateUniversity;
            }
            throw new Exception("Cannot unmarshal type Credit");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Credit)untypedValue;
            switch (value)
            {
                case Credit.Empty:
                    serializer.Serialize(writer, "");
                    return;
                case Credit.ApPhotoChrisRugaber:
                    serializer.Serialize(writer, "AP Photo\\/Chris Rugaber");
                    return;
                case Credit.GettyPaigeVickersForVox:
                    serializer.Serialize(writer, "Getty\\/Paige Vickers for Vox");
                    return;
                case Credit.JetskeFlickr:
                    serializer.Serialize(writer, "Jetske\\/Flickr");
                    return;
                case Credit.NickStockton:
                    serializer.Serialize(writer, "Nick Stockton");
                    return;
                case Credit.PaulineDakinPhotoPenguin:
                    serializer.Serialize(writer, "Pauline Dakin (Photo: Penguin)");
                    return;
                case Credit.PhotoIllustrationBrU00E1UlioAmado:
                    serializer.Serialize(writer, "Photo illustration  Br\\u00e1ulio Amado");
                    return;
                case Credit.PhotoIllustrationTheCutPhotosGettyImages:
                    serializer.Serialize(writer, "Photo-Illustration: The Cut; Photos Getty Images");
                    return;
                case Credit.PhotoDaveBenettGettyImages:
                    serializer.Serialize(writer, "Photo: Dave Benett\\/Getty Images");
                    return;
                case Credit.PhotoMarcPiaseckiWireImage:
                    serializer.Serialize(writer, "Photo: Marc Piasecki\\/WireImage");
                    return;
                case Credit.UsgsWashingtonStateUniversity:
                    serializer.Serialize(writer, "USGS\\/Washington State University");
                    return;
            }
            throw new Exception("Cannot marshal type Credit");
        }

        public static readonly CreditConverter Singleton = new CreditConverter();
    }

    internal class LangConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Lang) || t == typeof(Lang?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "":
                    return Lang.Empty;
                case "en":
                    return Lang.En;
            }
            throw new Exception("Cannot unmarshal type Lang");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Lang)untypedValue;
            switch (value)
            {
                case Lang.Empty:
                    serializer.Serialize(writer, "");
                    return;
                case Lang.En:
                    serializer.Serialize(writer, "en");
                    return;
            }
            throw new Exception("Cannot marshal type Lang");
        }

        public static readonly LangConverter Singleton = new LangConverter();
    }

    internal class TagConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Tag) || t == typeof(Tag?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "cracked: all posts":
                    return Tag.CrackedAllPosts;
                case "ifttt":
                    return Tag.Ifttt;
            }
            throw new Exception("Cannot unmarshal type Tag");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Tag)untypedValue;
            switch (value)
            {
                case Tag.CrackedAllPosts:
                    serializer.Serialize(writer, "cracked: all posts");
                    return;
                case Tag.Ifttt:
                    serializer.Serialize(writer, "ifttt");
                    return;
            }
            throw new Exception("Cannot marshal type Tag");
        }

        public static readonly TagConverter Singleton = new TagConverter();
    }
}
