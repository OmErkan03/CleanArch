namespace Uludag.Core.Caching.Redis
{
    // appsettings'teki "RedisOptions" bölümünün karşılığı.
    public class RedisOptions
    {
        public const string SectionName = "RedisOptions";

        // StackExchange.Redis bağlantı cümlesi.
        //   Tek sunucu : "localhost:6379,password=uludag123,abortConnect=false"
        //   Sentinel   : "localhost:26379,localhost:26380,localhost:26381,serviceName=mymaster,password=uludag123,abortConnect=false"
        // Sentinel modunda adresler Redis'in değil sentinel'lerin adresleridir. "serviceName" verildiğinde
        // StackExchange.Redis master'ın adresini sentinel'lerden öğrenir; failover olursa yeni master'a kendiliğinden geçer.
        // Sentinel'lerden biri kapalı olsa bile diğerleri üzerinden bağlanılabilir.
        public string ConnectionString { get; set; } = string.Empty;

        // Tüm anahtarların başına eklenir (ör. "ecommerce:"). Aynı Redis'i paylaşan uygulamaların anahtarları çakışmaz.
        public string InstanceName { get; set; } = string.Empty;
    }
}
