using ECommerce.IntegrationTests.Infrastructure;

// Tek bir ApiFactory (= bir PostgreSQL + bir Redis container'ı) tüm test çalıştırması boyunca paylaşılır.
[assembly: AssemblyFixture(typeof(ApiFactory))]

// Bu projedeki TÜM testler entegrasyon testidir: scripts/check.conf bu trait ile ayırır
// (fast profil: --filter-not-trait, full profil: --filter-trait). Bkz. docs/testing.md.
[assembly: Trait("Category", "Integration")]
